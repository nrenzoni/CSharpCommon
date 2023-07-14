using System.IO.Compression;
using Grpc.Net.Client;
using Grpc.Net.Compression;
using Microsoft.Extensions.Logging;

namespace GrpcShared;

public class GrpcClientCommon
{
    public static GrpcChannel BuildGrpcChannel(
        string connectionString)
    {
        var loggerFactory = LoggerFactory.Create(
            logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Trace);
            });

        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        var compressionProviders = new List<ICompressionProvider>
        {
            new BrotliCompressionProvider(CompressionLevel.Fastest),
            new GzipCompressionProvider(CompressionLevel.Optimal)
        };

        var channel = GrpcChannel.ForAddress(
            connectionString,
            new GrpcChannelOptions
            {
                LoggerFactory = loggerFactory,
                CompressionProviders = compressionProviders,
                HttpHandler = httpClientHandler,
                DisposeHttpClient = true,
            });

        return channel;
    }
}
