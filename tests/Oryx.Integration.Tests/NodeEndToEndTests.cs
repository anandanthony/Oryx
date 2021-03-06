﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Oryx.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Oryx.Integration.Tests
{
    public class NodeEndToEndTestsBase : PlatformEndToEndTestsBase
    {
        public readonly int ContainerPort = 3000;
        public readonly string DefaultStartupFilePath = "./run.sh";
        public readonly ITestOutputHelper _output;
        public readonly string _hostSamplesDir;
        public readonly string _tempRootDir;

        public NodeEndToEndTestsBase(ITestOutputHelper output, TestTempDirTestFixture testTempDirTestFixture)
        {
            _output = output;
            _hostSamplesDir = Path.Combine(Directory.GetCurrentDirectory(), "SampleApps");
            _tempRootDir = testTempDirTestFixture.RootDirPath;
        }
    }

    public class NodeOtherEndtoEndTests : NodeEndToEndTestsBase
    {
        public NodeOtherEndtoEndTests(ITestOutputHelper output, TestTempDirTestFixture testTempDirTestFixture)
            : base(output, testTempDirTestFixture)
        {
        }

        [Fact]
        public async Task CanBuildAndRunNodeApp_UsingZippedNodeModules_WithoutExtracting()
        {
            // NOTE:
            // 1. Use intermediate directory(which here is local to container) to avoid errors like
            //      "tar: node_modules/form-data: file changed as we read it"
            //    related to zipping files on a folder which is volume mounted.
            // 2. Use output directory within the container due to 'rsync'
            //    having issues with volume mounted directories

            // Arrange
            var appOutputDirPath = Directory.CreateDirectory(Path.Combine(_tempRootDir, Guid.NewGuid().ToString("N")))
                .FullName;
            var appOutputDirVolume = DockerVolume.Create(appOutputDirPath);
            var appOutputDir = appOutputDirVolume.ContainerDir;
            var nodeVersion = "10.14";
            var appName = "webfrontend";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var runAppScript = new ShellScriptBuilder()
                .AddCommand($"cd {appOutputDir}")
                .AddCommand("mkdir -p node_modules")
                .AddCommand("tar -xzf node_modules.tar.gz -C node_modules")
                .AddCommand($"oryx -bindPort {ContainerPort} -skipNodeModulesExtraction")
                .AddCommand(DefaultStartupFilePath)
                .AddDirectoryDoesNotExistCheck("/node_modules")
                .ToString();

            var buildScript = new ShellScriptBuilder()
                .AddCommand(
                $"oryx build {appDir} -i /tmp/int -o /tmp/out -l nodejs " +
                $"--language-version {nodeVersion} -p compress_node_modules=tar-gz")
                .AddCommand($"cp -rf /tmp/out/* {appOutputDir}")
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                new List<DockerVolume> { appOutputDirVolume, volume },
                "/bin/sh",
                new[]
                {
                    "-c",
                    buildScript
                },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    runAppScript
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Say It Again", data);
                });
        }

        [Fact]
        public async Task CanBuildAndRunNodeApp_OnSecondBuild_AfterZippingNodeModules_InFirstBuild()
        {
            // NOTE:
            // 1. Use intermediate directory(which here is local to container) to avoid errors like
            //      "tar: node_modules/form-data: file changed as we read it"
            //    related to zipping files on a folder which is volume mounted.
            // 2. Use output directory within the container due to 'rsync'
            //    having issues with volume mounted directories

            // Arrange
            var appOutputDirPath = Directory.CreateDirectory(Path.Combine(_tempRootDir, Guid.NewGuid().ToString("N")))
                .FullName;
            var appOutputDirVolume = DockerVolume.Create(appOutputDirPath);
            var appOutputDir = appOutputDirVolume.ContainerDir;
            var nodeVersion = "10.14";
            var appName = "webfrontend";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appOutputDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            var buildScript = new ShellScriptBuilder()
                .AddCommand(
                $"oryx build {appDir} -i /tmp/int -o /tmp/out -l nodejs " +
                $"--language-version {nodeVersion} -p compress_node_modules=tar-gz")
                .AddCommand($"oryx build {appDir} -i /tmp/int -o /tmp/out -l nodejs --language-version {nodeVersion}")
                .AddCommand($"cp -rf /tmp/out/* {appOutputDir}")
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                new List<DockerVolume> { appOutputDirVolume, volume },
                "/bin/sh",
                new[]
                {
                    "-c",
                    buildScript
                },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Say It Again", data);
                });
        }

        [Fact]
        public async Task CanBuildAndRunNodeApp_OnSecondBuild_AfterNotZippingNodeModules_InFirstBuild()
        {
            // NOTE:
            // 1. Use intermediate directory(which here is local to container) to avoid errors like
            //      "tar: node_modules/form-data: file changed as we read it"
            //    related to zipping files on a folder which is volume mounted.
            // 2. Use output directory within the container due to 'rsync' 
            //    having issues with volume mounted directories

            // Arrange
            var appOutputDirPath = Directory.CreateDirectory(Path.Combine(_tempRootDir, Guid.NewGuid().ToString("N")))
                .FullName;
            var appOutputDirVolume = DockerVolume.Create(appOutputDirPath);
            var appOutputDir = appOutputDirVolume.ContainerDir;
            var nodeVersion = "10.14";
            var appName = "webfrontend";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appOutputDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            var buildScript = new ShellScriptBuilder()
                .AddCommand($"oryx build {appDir} -i /tmp/int -o /tmp/out -l nodejs --language-version {nodeVersion}")
                .AddCommand(
                $"oryx build {appDir} -i /tmp/int -o /tmp/out -l nodejs " +
                $"--language-version {nodeVersion} -p compress_node_modules=tar-gz")
                .AddCommand($"cp -rf /tmp/out/* {appOutputDir}")
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                new List<DockerVolume> { appOutputDirVolume, volume },
                "/bin/sh",
                new[]
                {
                    "-c",
                    buildScript
                },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Say It Again", data);
                });
        }

        [Theory]
        [InlineData("10.10")]
        [InlineData("10.14")]
        public async Task CanBuildAndRun_NodeApp(string nodeVersion)
        {
            // Arrange
            var appName = "webfrontend";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx",
                new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Say It Again", data);
                });
        }

        [Fact]
        public async Task NodeStartupScript_UsesPortEnvironmentVariableValue()
        {
            // Arrange
            var nodeVersion = "10.14";
            var appName = "webfrontend";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"export PORT={ContainerPort}")
                .AddCommand($"oryx -appPath {appDir}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx",
                new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Say It Again", data);
                });
        }

        [Fact]
        public async Task NodeStartupScript_UsesSuppliedBindingPort_EvenIfPortEnvironmentVariableValue_IsPresent()
        {
            // Arrange
            var nodeVersion = "10.14";
            var appName = "webfrontend";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"export PORT=9095")
                .AddCommand($"oryx -appPath {appDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx",
                new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Say It Again", data);
                });
        }

        [Fact]
        public async Task CanBuildAndRunNodeApp_UsingYarnForBuild_AndExplicitOutputFile()
        {
            // Arrange
            var appName = "webfrontend-yarnlock";
            var nodeVersion = "10.14";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var startupFilePath = "/tmp/startup.sh";
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appDir} -output {startupFilePath} -bindPort {ContainerPort}")
                .AddCommand(startupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx", new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh", new[] { "-c", script },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Say It Again", data);
                });
        }

        // Run on Linux only as TypeScript seems to create symlinks and this does not work on Windows machines.
        [EnableOnPlatform("LINUX")]
        public async Task CanBuildNodeAppUsingScriptsNodeInPackageJson()
        {
            // Arrange
            var appName = "NodeAndTypeScriptHelloWorld";
            var nodeVersion = "10.14";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx",
                new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("{\"message\":\"Hello World!\"}", data);
                });
        }

        [Fact]
        public async Task Node_Lab2AppServiceApp()
        {
            // Arrange
            var appName = "lab2-appservice";
            var nodeVersion = "10.14";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx",
                new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Welcome to Express", data);
                });
        }

        [Fact]
        public async Task Node_SoundCloudNgrxApp()
        {
            // Arrange
            var appName = "soundcloud-ngrx";
            var nodeVersion = "8.11";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appDir} -bindPort {ContainerPort}")
                .AddCommand($"npm rebuild node-sass") //remove this once workitem 762584 is done
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx",
                new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var response = await _httpClient.GetAsync($"http://localhost:{hostPort}/");
                    Assert.True(response.IsSuccessStatusCode);
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("<title>SoundCloud • Angular2 NgRx</title>", data);
                });
        }

        [Fact]
        public async Task Node_CreateReactAppSample()
        {
            // Arrange
            var appName = "create-react-app-sample";
            var nodeVersion = "10.14";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx",
                new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("<title>React App</title>", data);
                });
        }

        [Theory]
        [InlineData("8.11")]
        [InlineData("8.12")]
        [InlineData("10.1")]
        [InlineData("10.10")]
        [InlineData("10.14")]
        public async Task Node_CreateReactAppSample_zippedNodeModules(string nodeVersion)
        {
            // Arrange
            // Use a separate volume for output due to rsync errors
            var appOutputDirPath = Directory.CreateDirectory(Path.Combine(_tempRootDir, Guid.NewGuid().ToString("N")))
                .FullName;
            var appOutputDirVolume = DockerVolume.Create(appOutputDirPath);
            var appOutputDir = appOutputDirVolume.ContainerDir;
            var appName = "create-react-app-sample";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var runAppScript = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appOutputDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            var buildScript = new ShellScriptBuilder()
               .AddCommand(
                $"oryx build {appDir} -i /tmp/int -o /tmp/out -l nodejs " +
                $"--language-version {nodeVersion} -p compress_node_modules=zip")
               .AddCommand($"cp -rf /tmp/out/* {appOutputDir}")
               .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                new List<DockerVolume> { appOutputDirVolume, volume },
                "/bin/bash",
                new[] { "-c", buildScript },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[] { "-c", runAppScript },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("<title>React App</title>", data);
                });
        }

        [Fact(Skip = "#824174: Sync the Node Go startup code with the C# 'run-script' code")]
        public async Task Node_CreateReactAppSample_singleImage()
        {
            // Arrange
            var appName = "create-react-app-sample";
            var nodeVersion = "10";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var runScript = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand(
                $"oryx run-script --appPath {appDir} --platform nodejs " +
                $"--platform-version {nodeVersion} --bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName: appName,
                output: _output,
                volume: volume,
                buildCmd: "oryx",
                buildArgs: new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                runtimeImageName: $"oryxdevms/build",
                ContainerPort,
                runCmd: "/bin/sh",
                runArgs: new[]
                {
                    "-c",
                    runScript
                },
                assertAction: async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("<title>React App</title>", data);
                });
        }

        [Fact(Skip = "#824174: Sync the Node Go startup code with the C# 'run-script' code")]
        public async Task CanBuildAndRun_NodeExpressApp_UsingSingleImage_AndCustomScript()
        {
            // Arrange
            var appName = "linxnodeexpress";
            var nodeVersion = "10";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;

            // Create a custom startup command
            const string customStartupScriptName = "customStartup.sh";
            File.WriteAllText(Path.Join(volume.MountedHostDir, customStartupScriptName),
                "#!/bin/bash\n" +
                $"PORT={ContainerPort} node server.js\n");

            var runScript = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"chmod -x ./{customStartupScriptName}")
                .AddCommand(
                $"oryx run-script --appPath {appDir} --platform nodejs " +
                $"--platform-version {nodeVersion} --userStartupCommand {customStartupScriptName} --debug")
                .AddCommand($"./{customStartupScriptName}")
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName: appName,
                output: _output,
                volume: volume,
                buildCmd: "oryx",
                buildArgs: new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                runtimeImageName: $"oryxdevms/build",
                ContainerPort,
                runCmd: "/bin/sh",
                runArgs: new[]
                {
                    "-c",
                    runScript
                },
                assertAction: async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Equal("Hello World from express!", data);
                });
        }

        [Fact(Skip = "#824174: Sync the Node Go startup code with the C# 'run-script' code")]
        public async Task CanBuildAndRun_NodeExpressApp_UsingSingleImage_AndCustomStartupCommandOnly()
        {
            // Arrange
            var appName = "linxnodeexpress";
            var nodeVersion = "10";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;

            // Create a custom startup command
            const string customStartupScriptCommand = "'npm start'";

            var runScript = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand(
                $"oryx run-script --appPath {appDir} --platform nodejs " +
                $"--platform-version {nodeVersion} --userStartupCommand {customStartupScriptCommand} --debug")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName: appName,
                output: _output,
                volume: volume,
                buildCmd: "oryx",
                buildArgs: new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                runtimeImageName: $"oryxdevms/build",
                ContainerPort,
                runCmd: "/bin/sh",
                runArgs: new[]
                {
                    "-c",
                    runScript
                },
                assertAction: async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Equal("Hello World from express!", data);
                });
        }
    }

    [Trait("category", "node")]
    public class NodeSassExampleTest : NodeEndToEndTestsBase
    {
        public NodeSassExampleTest(ITestOutputHelper output, TestTempDirTestFixture fixture)
            : base(output, fixture)
        {
        }

        [Theory]
        [MemberData(nameof(TestValueGenerator.GetNodeVersions), MemberType = typeof(TestValueGenerator))]
        public async Task Test_NodeSassExample(string nodeVersion)
        {
            // Arrange
            var appName = "node-sass-example";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var script = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                volume,
                "oryx",
                new[] { "build", appDir, "-l", "nodejs", "--language-version", nodeVersion },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    script
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("<title>Node-sass example</title>", data);
                });
        }
    }

    [Trait("category", "node")]
    public class NodeTestUsingZippedNodeModules : NodeEndToEndTestsBase
    {
        public NodeTestUsingZippedNodeModules(ITestOutputHelper output, TestTempDirTestFixture fixture)
            : base(output, fixture)
        {
        }

        [Theory]
        [MemberData(nameof(TestValueGenerator.GetZipOptions_NodeVersions), MemberType = typeof(TestValueGenerator))]
        public async Task CanBuildAndRunNodeApp_UsingZippedNodeModules(string compressFormat, string nodeVersion)
        {
            // NOTE:
            // 1. Use intermediate directory(which here is local to container) to avoid errors like
            //      "tar: node_modules/form-data: file changed as we read it"
            //    related to zipping files on a folder which is volume mounted.
            // 2. Use output directory within the container due to 'rsync'
            //    having issues with volume mounted directories

            // Arrange
            var appOutputDirPath = Directory.CreateDirectory(Path.Combine(_tempRootDir, Guid.NewGuid().ToString("N")))
                .FullName;
            var appOutputDirVolume = DockerVolume.Create(appOutputDirPath);
            var appOutputDir = appOutputDirVolume.ContainerDir;
            var appName = "linxnodeexpress";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var runAppScript = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appOutputDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            var buildScript = new ShellScriptBuilder()
                .AddCommand(
                $"oryx build {appDir} -i /tmp/int -o /tmp/out -l nodejs " +
                $"--language-version {nodeVersion} -p compress_node_modules={compressFormat}")
                .AddCommand($"cp -rf /tmp/out/* {appOutputDir}")
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                new List<DockerVolume> { appOutputDirVolume, volume },
                "/bin/sh",
                new[]
                {
                    "-c",
                    buildScript
                },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    runAppScript
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Equal("Hello World from express!", data);
                });
        }
    }

    [Trait("category", "node")]
    public class NodeTestWithAppInsightsConfigured : NodeEndToEndTestsBase
    {
        public NodeTestWithAppInsightsConfigured(ITestOutputHelper output, TestTempDirTestFixture fixture)
            : base(output, fixture)
        {
        }

        [Theory]
        [MemberData(nameof(TestValueGenerator.GetNodeVersions_SupportDebugging),
            MemberType = typeof(TestValueGenerator))]
        public async Task CanBuildAndRun_NodeApp_WithAppInsights_Configured(string nodeVersion)
        {
            // Arrange
            var appName = "linxnodeexpress";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var spcifyNodeVersionCommand = "-l nodejs --language-version=" + nodeVersion;
            var aIKey = "APPINSIGHTS_INSTRUMENTATIONKEY";
            var buildScript = new ShellScriptBuilder()
                .AddCommand($"oryx build {appDir} -o {appDir} {spcifyNodeVersionCommand} --log-file {appDir}/1.log")
                .AddDirectoryExistsCheck($"{appDir}/node_modules")
                .AddFileExistsCheck($"{appDir}/oryx-appinsightsloader.js")
                .AddFileExistsCheck($"{appDir}/oryx-manifest.toml")
                .AddStringExistsInFileCheck("injectedAppInsights=\"True\"", $"{appDir}/oryx-manifest.toml")
                .ToString();

            var runScript = new ShellScriptBuilder()
                .AddCommand($"export {aIKey}=asdas")
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appDir} -bindPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .AddFileExistsCheck($"{appDir}/oryx-appinsightsloader.js")
                .AddFileExistsCheck($"{appDir}/oryx-manifest.toml")
                .AddStringExistsInFileCheck("injectedAppInsights=\"True\"", $"{appDir}/oryx-manifest.toml")
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                new List<DockerVolume> { volume },
                "/bin/bash",
                 new[]
                {
                    "-c",
                    buildScript
                },
                $"oryxdevms/node-{nodeVersion}",
                new List<EnvironmentVariable> { new EnvironmentVariable(aIKey, "asdasda") },
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    runScript
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Hello World from express!", data);
                });
        }
    }

    [Trait("category", "node")]
    public class NodeTestBuildAndRunAppWithDebugger : NodeEndToEndTestsBase
    {
        public NodeTestBuildAndRunAppWithDebugger(ITestOutputHelper output, TestTempDirTestFixture fixture)
            : base(output, fixture)
        {
        }

        [Theory]
        [MemberData(
           nameof(TestValueGenerator.GetNodeVersions_SupportDebugging),
           MemberType = typeof(TestValueGenerator))]
        public async Task CanBuildAndRunNodeApp_WithDebugger(string nodeVersion)
        {
            // Arrange
            var appOutputDirPath = Directory.CreateDirectory(Path.Combine(_tempRootDir, Guid.NewGuid().ToString("N")))
                .FullName;
            var appOutputDirVolume = DockerVolume.Create(appOutputDirPath);
            var appOutputDir = appOutputDirVolume.ContainerDir;
            var appName = "linxnodeexpress";
            var hostDir = Path.Combine(_hostSamplesDir, "nodejs", appName);
            var volume = DockerVolume.Create(hostDir);
            var appDir = volume.ContainerDir;
            var runAppScript = new ShellScriptBuilder()
                .AddCommand($"cd {appDir}")
                .AddCommand($"oryx -appPath {appOutputDir} -remoteDebug -debugPort {ContainerPort}")
                .AddCommand(DefaultStartupFilePath)
                .ToString();

            var buildScript = new ShellScriptBuilder()
                .AddCommand(
                $"oryx build {appDir} -i /tmp/int -o /tmp/out -l nodejs " +
                $"--language-version {nodeVersion} -p compress_node_modules=tar-gz")
                .AddCommand($"cp -rf /tmp/out/* {appOutputDir}")
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                appName,
                _output,
                new List<DockerVolume> { appOutputDirVolume, volume },
                "/bin/sh",
                new[]
                {
                    "-c",
                    buildScript
                },
                $"oryxdevms/node-{nodeVersion}",
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    runAppScript
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/json/list");
                    Assert.Contains("devtoolsFrontendUrl", data);
                });
        }
    }
}