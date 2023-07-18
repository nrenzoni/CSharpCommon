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

public abstract class GrpcClientWrapperBase
{
    private readonly ClientBase _client;

    protected GrpcClientWrapperBase(
        ClientBase client)
    {
        _client = client;
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

        var clientType = _client.GetType();

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
            _client,
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

        var clientType = _client.GetType();

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
            _client,
            args)!;

        await resultTask;

        var resultResult = resultTask.Result;

        return outGrpcCallConverter(resultResult);
    }
    
    
}
