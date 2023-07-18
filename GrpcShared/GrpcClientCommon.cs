using System.IO.Compression;
using System.Runtime.CompilerServices;
using Grpc.Core;
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

public abstract class GrpcClientWrapperBase<T> : IDisposable
    where T : ClientBase<T>
{
    private ClientBase<T>? _client;

    private GrpcChannel? _grpcChannel;

    protected void DelayedInitClient(
        GrpcChannel channel,
        ClientBase<T> client)
    {
        _grpcChannel = channel;
        _client = client;
    }

    private ClientBase<T> Client
    {
        get
        {
            if (_client == null)
                throw new Exception($"Must call {nameof(DelayedInitClient)} before accessing {nameof(Client)}.");
            return _client;
        }
    }

    // no input arg and matching converter
    protected async Task<TReturnConverted> RequestAndConvertInternal<TReturn, TReturnConverted>(
        Func<TReturn, TReturnConverted> outGrpcCallConverter,
        [CallerMemberName] string methodName = "")
    {
        const string asyncKeyword = "Async";
        if (!methodName.EndsWith(asyncKeyword))
            throw new Exception($"Only methods ending with {asyncKeyword} currently supported.");

        object[]? args = null;

        var clientType = Client.GetType();

        var methodInfo = clientType.GetMethod(methodName);

        if (methodInfo is null)
            throw new Exception($"Method {methodName} not found in type {clientType}.");

        var methodParameterInfos = methodInfo.GetParameters();

        if (methodParameterInfos.Any())
        {
            throw new Exception(
                $"Method {methodName} has {methodParameterInfos.Length} parameters, but was attempted to be invoked with none.");
        }

        var resultTask = (Task<TReturn>)methodInfo.Invoke(
            Client,
            args)!;

        await resultTask;

        var resultResult = resultTask.Result;

        return outGrpcCallConverter(resultResult);
    }

    protected async Task<TReturnConverted> RequestAndConvertInternal<TArgIn, TArgConverted, TReturn, TReturnConverted>(
        TArgIn? inputArgs,
        Func<TArgIn, TArgConverted>? inArgsConverter,
        Func<TReturn, TReturnConverted> outGrpcCallConverter,
        [CallerMemberName] string methodName = "")
    {
        const string asyncKeyword = "Async";
        if (!methodName.EndsWith(asyncKeyword))
            throw new Exception($"Only methods ending with {asyncKeyword} currently supported.");

        object[]? args = null;

        if (inputArgs != null)
        {
            if (inArgsConverter is null)
                throw new ArgumentException();

            args = new object[]
            {
                inArgsConverter(inputArgs)
            };
        }

        var clientType = Client.GetType();

        var methodInfo = clientType.GetMethod(methodName);

        if (methodInfo is null)
            throw new Exception($"Method {methodName} not found in type {clientType}.");

        var methodParameterInfos = methodInfo.GetParameters();

        if (methodParameterInfos.Length != 1)
        {
            throw new Exception(
                $"Method {methodName} has {methodParameterInfos.Length} parameters, but was attempted to be invoked with one.");
        }

        var resultTask = (Task<TReturn>)methodInfo.Invoke(
            Client,
            args)!;

        await resultTask;

        var resultResult = resultTask.Result;

        return outGrpcCallConverter(resultResult);
    }

    public void Dispose()
    {
        _grpcChannel?.Dispose();
    }
}
