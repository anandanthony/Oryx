﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Oryx.Common;

namespace Microsoft.Oryx.Tests.Common
{
    public class DockerRunCommandResult : DockerCommandResult
    {
        public DockerRunCommandResult(
            string containerName,
            int exitCode,
            Exception exception,
            string output,
            string error,
            List<DockerVolume> volumes,
            string executedRunCommand)
            : base(exitCode, exception, output, error, executedRunCommand)
        {
            ContainerName = containerName;
            Volumes = volumes;
        }

        public string ContainerName { get; }

        private List<DockerVolume> Volumes { get; }

        public override string GetDebugInfo(IDictionary<string, string> extraDefs = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine(base.GetDebugInfo(extraDefs));
            sb.AppendLine();

            sb.AppendLine("Use the following commands to investigate the failed container:");
            sb.AppendLine($"docker logs {ContainerName}");
            sb.AppendLine();
            sb.AppendLine($"docker commit {ContainerName} investigate_{ContainerName}");
            sb.AppendLine();

            var volumeList = string.Empty;
            if (Volumes?.Count > 0)
            {
                volumeList = string.Join(" ", Volumes.Select(kvp => $"-v {kvp.MountedHostDir}:/{kvp.ContainerDir.TrimStart('/')}"));
            }
            sb.AppendLine($"docker run -it {volumeList} investigate_{ContainerName} /bin/bash");

            return sb.ToString();
        }
    }
}