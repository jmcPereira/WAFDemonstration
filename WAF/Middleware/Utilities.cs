using System.IO.Compression;

namespace WAF.Middleware
{
    public class Utilities
    {
        public static void CopyHeadersIntoResponse(HttpContext context, HttpResponseMessage proxyResponse)
        {
            // Copy the proxy response back to the original response
            context.Response.StatusCode = (int)proxyResponse.StatusCode;

            // Copy headers
            foreach (var header in proxyResponse.Headers) context.Response.Headers[header.Key] = header.Value.ToArray();

            foreach (var header in proxyResponse.Content.Headers) context.Response.Headers[header.Key] = header.Value.ToArray();

            context.Response.Headers["Content-Encoding"] = "identity";
            context.Response.Headers.Remove("Content-Length");
            // Remove the Transfer-Encoding header (if present) to avoid issues
            context.Response.Headers.Remove("Transfer-Encoding");
        }

        public static async Task<string> DecodeResponse(HttpResponseMessage proxyResponse)
        {
            // Check if the response is compressed
            Stream responseStream = await proxyResponse.Content.ReadAsStreamAsync();
            if (proxyResponse.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else if (proxyResponse.Content.Headers.ContentEncoding.Contains("deflate"))
            {
                responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
            }
            else if (proxyResponse.Content.Headers.ContentEncoding.Contains("br"))
            {
                responseStream = new BrotliStream(responseStream, CompressionMode.Decompress);
            }

            // Read the response body
            using var reader = new StreamReader(responseStream);
            var responseBody = await reader.ReadToEndAsync();
            return responseBody;
        }

        public static Stream WriteToStream(string decodedBody)
        {
            try
            {
                MemoryStream stream = new();
                using StreamWriter writer = new(stream);
                writer.Write(decodedBody);
                writer.Flush(); // Ensure all data is written before using the stream
                stream.Position = 0; // Reset position for reading
                return stream;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error here:" + e.Message);
            }

            return null;
        }
    }
}
