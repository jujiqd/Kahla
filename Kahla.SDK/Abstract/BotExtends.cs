﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kahla.SDK.Abstract
{
    public static class BotExtends
    {
        private static IEnumerable<Type> ScanBots()
        {
            var bots = Assembly
                .GetEntryAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BotBase)));
            return bots;
        }

        private static IEnumerable<Type> ScanHandler()
        {
            var handlers = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsInterface)
                .Where(t => t.GetInterfaces().Contains(typeof(ICommandHandler)));
            return handlers;
        }

        public static IServiceCollection AddBots(this IServiceCollection services)
        {
            // Register the bots.
            foreach (var botType in ScanBots())
            {
                services.AddScoped(botType);
            }
            foreach(var handler in ScanHandler())
            {
                services.AddScoped(typeof(ICommandHandler), handler.MakeGenericType(typeof(BotBase)));
            }
            services.AddScoped(typeof(BotHost<>));
            services.AddScoped(typeof(BotCommander<>));
            services.AddScoped(typeof(BotFactory<>));
            return services;
        }
    }
}
