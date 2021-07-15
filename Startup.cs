using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace merge_test
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        //add comments on master branch

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(LicenseMappingProfile));
            var dynamoDbConfig = Configuration.GetSection("DynamoDb");
            var runLocalDynamoDb = dynamoDbConfig.GetValue<bool>("LocalMode");

            if (runLocalDynamoDb)
            {
                services.AddSingleton<IAmazonDynamoDB>(sp =>
                {
                    var clientConfig = new AmazonDynamoDBConfig { ServiceURL = dynamoDbConfig.GetValue<string>("LocalServiceUrl") };
                    return new AmazonDynamoDBClient(clientConfig);
                });
            }
            else
            {
                services.AddAWSService<IAmazonDynamoDB>();
            }

            services.AddTransient<ILicenseProvider, LicenseProvider>();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new FilterOptionValueJsonSerializer());
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
               
                var apiUrl = Environment.GetEnvironmentVariable("ApiUrl");
                if (apiUrl != null)
                {
                    c.AddServer(new OpenApiServer
                    {
                        Url = apiUrl
                    });
                }

                c.IncludeXmlComments(xmlPath);
                c.DocumentFilter<AwsApiGatewayCorsFilter>();
                c.OperationFilter<AwsApiGatewayIntegrationFilter>();
                c.OperationFilter<AwsApiGatewayRequestValidatorFilter>();
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.AddSecurityDefinition("CognitoAuthorizer", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        ["x-amazon-apigateway-authtype"] = new OpenApiString("cognito_user_pools"),
                        ["x-amazon-apigateway-authorizer"] = new OpenApiObject
                        {
                            ["type"] = new OpenApiString("cognito_user_pools"),
                            ["providerARNs"] = new OpenApiArray
                            {
                                new OpenApiString("!Sub ${CognitoUserPoolArn}")
                            }
                        }
                    }
                });
            });

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("v1/swagger.yaml", "Licensing API v1 YAML");
                    c.SwaggerEndpoint("v1/swagger.json", "Licensing API v1 JSON");
                });

            app
                .UseSerilogRequestLogging()
                .UseHttpsRedirection()
                .UseRouting()
                .UseCors()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapGet("/", async context =>
                    {
                        await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
                    });
                });
        }
    }
}
