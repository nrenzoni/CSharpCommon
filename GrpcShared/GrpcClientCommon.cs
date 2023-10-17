using System.Collections;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using CustomShared;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Compression;
using Microsoft.Extensions.Logging;
using Type = System.Type;

namespace GrpcShared;

public class GrpcClientCommon
{
    public static GrpcChannel BuildGrpcChannel(string connectionString)
    {
        var loggerFactory = LoggerFactory.Create(
            logging => {
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

    public static bool AllPropertiesNullOrEmpty<T>(T inObj)
        where T : class?
    {
        if (inObj == null)
            return false;

        foreach (var propertyInfo in
                 typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var type = propertyInfo.PropertyType;

            if (type.IsPrimitive || type.IsEnum)
            {
                return false;
            }

            var value = propertyInfo.GetValue(inObj);

            if (value.IsEnumerable())
            {
                var x = value as ICollection;
                if (x.Count != 0)
                    return false;
            }
            else if (value is IMessage imsg)
            {
                var innerMsgNullOrEmpty
                    = AllPropertiesNullOrEmpty(imsg);

                if (!innerMsgNullOrEmpty)
                    return false;
            }
        }

        return true;
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
        return await RequestAndConvertInternal<object, object, TReturn, TReturnConverted>(
            null,
            null,
            outGrpcCallConverter,
            methodName);
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

        var clientType = Client.GetType();

        var methodInfos = clientType.GetMethods();

        var matchingMethods = methodInfos.Where(
                x => x.Name == methodName
                     && x.GetParameters().Length > 2)
            .ToList();

        if (matchingMethods.Count != 1)
            throw new Exception($"No matching service method found for {methodName}.");

        var methodInfo = matchingMethods.First();

        if (methodInfo is null)
            throw new Exception($"Method {methodName} not found in type {clientType}.");

        object[] args;

        if (inputArgs != null)
        {
            if (inArgsConverter is null)
                throw new ArgumentException();

            args = new object[]
            {
                inArgsConverter(inputArgs),
                Type.Missing,
                Type.Missing,
                Type.Missing
            };
        }
        else
        {
            var firstParameterType = methodInfo.GetParameters().First().ParameterType;
            if (firstParameterType != typeof(Empty))
                throw new Exception($"Method Not supported");

            // empty protobuf request
            args = new object[]
            {
                new Empty(),
                Type.Missing,
                Type.Missing,
                Type.Missing
            };
        }

        var resultTask = (AsyncUnaryCall<TReturn>)methodInfo.Invoke(
            Client,
            args)!;

        var result = await resultTask;

        return outGrpcCallConverter(result);
    }

    public void Dispose()
    {
        _grpcChannel?.Dispose();
    }
}