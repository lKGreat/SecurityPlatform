using System;
using System.Reflection;
using Atlas.Application;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VSDiagnostics;

namespace Atlas.Application.Performance;
[CPUUsageDiagnoser]
public class StartupRegistrationBenchmarksSuite4
{
    private Assembly[] _assemblies = null!;
    [GlobalSetup]
    public void Setup()
    {
        _assemblies = AppDomain.CurrentDomain.GetAssemblies();
    }

    [Benchmark]
    public IServiceCollection AddAtlasApplication()
    {
        var services = new ServiceCollection();
        return services.AddAtlasApplication();
    }

    [Benchmark]
    public IServiceCollection AddAutoMapperAllLoadedAssemblies()
    {
        var services = new ServiceCollection();
        services.AddAutoMapper(_assemblies);
        return services;
    }
}