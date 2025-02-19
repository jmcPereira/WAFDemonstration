using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing;
using WAFRuleModels;

namespace WAF.Middleware;

public class WafMiddleware(RequestDelegate _, IServiceProvider serviceProvider)
{
    private static readonly string ServerAddress = Environment.GetEnvironmentVariable("SERVER_ADDRESS") 
                                                   ?? "http://localhost:9748/";
    public async Task InvokeAsync(HttpContext context)
    {
        // Retrieve rules from the database
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WafRuleDbContext>();
        var rules = await dbContext.WafRules.ToListAsync();

        // Check request body
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var decodedBody = HttpUtility.UrlDecode(requestBody);

        // Apply request rules
        if (ApplyRules(context, rules, ref decodedBody, TrafficDirection.Inbound))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync($"Blocked by WAF!");
        }

        // Proxy the request to another application
        var httpClient = new HttpClient();
        var proxyUri = new Uri(ServerAddress.TrimEnd('/') + context.Request.Path + context.Request.QueryString);
        // Create a proxy request
        var requestBodyBytes = Encoding.UTF8.GetBytes(decodedBody);
        var requestBodyStream = new MemoryStream(requestBodyBytes);
        var proxyRequest = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = proxyUri,
            Content = new StreamContent(requestBodyStream)
        };

        // Copy headers from the original request to the proxy request
        foreach (var header in context.Request.Headers)
        {
            if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                proxyRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
        
        proxyRequest.Content.Headers.Remove("Content-Length");
        // Send the proxy request
        HttpResponseMessage proxyResponse;
        try
        {
            proxyResponse = await httpClient.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e.ToString());
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync("Unable to connect to the target application.");
            return;
        }

        var responseBody = await Utilities.DecodeResponse(proxyResponse);

        // Apply response rules
        if (ApplyRules(context, rules, ref responseBody, TrafficDirection.Outbound))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync($"Blocked by WAF!");
        }

        Utilities.CopyHeadersIntoResponse(context, proxyResponse);
        
        // Write the response body
        await context.Response.WriteAsync(responseBody);

    }

    private static bool ApplyRules(HttpContext context, List<WafRule> rules, ref string decodedBody, TrafficDirection direction)
    {
        foreach (var rule in rules.Where(r => r.TrafficDirectionKind == direction))
        {
            var matches = Regex.Matches(decodedBody, rule.Pattern);
            if (matches.Any())
            {
                if (rule.WafAction == WafAction.Block)
                    return true;
                if (rule.WafAction is WafAction.CensorElement or WafAction.Troll)
                    foreach (Match match in matches)
                        decodedBody = decodedBody
                            .Replace(match.Value, rule.WafAction == WafAction.Troll
                                ? rule.TrollReplacement
                                : "Censored By WAF!");
            }
        }

        return false;
    }
}