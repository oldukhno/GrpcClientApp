using Autofac;
using Autofac.Extensions.DependencyInjection;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using GrpcService;

var builder = WebApplication.CreateBuilder(args);

const string Name = "Greeter";

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var containerBuilder = new ContainerBuilder();

var rootContainer = containerBuilder.Build(); 

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

app.MapGet("/greetings", () =>
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGrpcClient<Greeter.GreeterClient>(Name, options =>
            {
                options.Address = new Uri("http://localhost:5160");
                options.ChannelOptionsActions.Add(channelOptions =>
                {
                    channelOptions.Credentials = ChannelCredentials.Insecure;
                });
            });

        var scope = rootContainer.BeginLifetimeScope(builder =>
        {
            builder.Populate(serviceCollection);
        });

        var grpcClientFactory = scope.Resolve<GrpcClientFactory>();
        var grpcClient = grpcClientFactory.CreateClient<Greeter.GreeterClient>(Name);

        var reply = grpcClient.SayHello(new HelloRequest { Name = "Neo" });

        scope.Dispose();

        // check that we did not dispose the channels here

        return reply.Message;
    })
    .WithName("GetGreetings")
    .WithOpenApi();

app.Run();