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

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders();
            app.UseHttpsRedirection();

            app.UseResponseCompression();

            app.Use(async (context, next) =>
            {
                var url = context.Request.Path.Value;

                // Rewrite to index
                if (url == "/")
                {
                    context.Request.Path = "/index.html";
                }

                await next();
            });
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new S3FileProvider(new AmazonS3Client(Amazon.RegionEndpoint.USWest2),
                "s3-test-prefix")
            });

        }
    }
}
