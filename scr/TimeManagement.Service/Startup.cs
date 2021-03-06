﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using TimeManagement.Data;
using TimeManagement.Service;
using TimeManagement.Streaming.Consumer;

namespace TimeManagement
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var config = builder.Build();

            services.AddTransient<IEmployeeProvider>(p => new EmployeeProvider(config["ConnectionString:TimeManagement"]));
            services.AddTransient<IEmployeeProcessor>(p => new EmployeeProcessor(config["ConnectionString:TimeManagement"]));

            services.AddSingleton<IBookingStream, BookingStream>();
            services.AddSingleton<Action<string>>(m => Console.WriteLine(m));
            services.AddSingleton<IBookingConsumer, BookingConsumer>();

            services.AddSingleton<BookingMessageRelay>();

            services.AddCors(option => option.AddPolicy("CorsPolicy", p => p.AllowAnyHeader()));

            services.AddSignalR();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors("CorsPolicy");

            app.UseSignalR(configure => configure.MapHub<BookingHub>("bookingHub"));

            app.UseMvc();

            app.ApplicationServices.GetService<BookingMessageRelay>();
            Task.Factory.StartNew(() => app.ApplicationServices.GetService<IBookingConsumer>().Listen());
        }
    }
}
