# Oryx

[![Build Status](https://devdiv.visualstudio.com/DevDiv/_apis/build/status/Oryx/Oryx-BuildImage?branchName=master)](https://devdiv.visualstudio.com/DevDiv/_build/latest?definitionId=9922?branchName=master)

Oryx is a build system which automatically compiles source code repos into
runnable artifacts. It is used to build web apps for [Azure App Service][] and
other platforms.

To receive updates on runtimes and versions supported by Oryx and App Service,
subscribe to [Azure Updates][] or watch the
[github.com/Azure/app-service-announcements](https://github.com/Azure/app-service-announcements)
tracker.

[Azure App Service]: https://azure.microsoft.com/services/app-service/
[Azure Updates]: https://azure.microsoft.com/updates

Oryx generates and runs an opinionated build script within a *build* container
based on analysis of a codebase's contents. For example, if `package.json` is
discovered in the repo Oryx includes `npm run build` in the build script; or if
`requirements.txt` is found it includes `pip install -r requirements.txt`.

Oryx also generates a run-time startup script for the app including typical
start commands like  `npm run start` for Node.js or a WSGI module and server
for Python.

The built artifacts and start script are loaded into a minimalistic *run*
container and run.

# Supported runtimes

Runtime | Version
--------|--------
Python  | 2.7<br />3.6, 3.7
Node.js | 4.4, 4.5, 4.8<br />6.2, 6.6, 6.9, 6.10, 6.11<br />8.0, 8.1, 8.2, 8.8, 8.9, 8.11, 8.12<br />9.4<br />10.1, 10.10, 10.14, 10.15
.NET Core | 1.0, 1.1<br />2.0, 2.1, 2.2
PHP     | 5.6<br />7.0, 7.2, 7.3

Patches (0.0.**x**) are applied as soon as possible after they are released upstream.

# Get started

Though built first for use within Azure services, you can also use the Oryx
build system yourself for troubleshooting and tests. Following are simple
instructions; for complete background see our [architecture
doc](./doc/architecture.md).

Oryx includes two command-line applications; the first is included in the
*build* image and generates a build script by analyzing a codebase. The second
is included in *run* images and generates a startup script. Both are aliased
and accessible as `oryx` in their respective images.

### `oryx build`

When `oryx build` is run, the system detects which programming platforms appear
to be in use and applies toolsets appropriate for each one. You can override
the default choices through [configuration](./doc/configuration.md#oryx-configuration).

The `--output` (or `-o`)  parameter specifies where prepared artifacts will be
placed; if not specified the source directory is used for output as well.

For all options, specify `oryx --help`.

### `oryx -appPath`

When `oryx` is run in the runtime images it generates a start script named
run.sh, by default in the same folder as the compiled artifact.

## Build and run an app

To build and run an app from a repo, follow these approximate steps. An example
script follows.

1. Mount the repo as a volume in Oryx's `docker.io/oryxprod/build` container.
1. Run `oryx build ...` within the repo directory to build a runnable artifact.
1. Mount the output directory from build in an appropriate Oryx "run"
   container, such as `docker.io/oryxprod/node-10.14`.
1. Run `oryx ...` within the "run" container to write a startup script.
1. Run the generated startup script, by default `/run.sh`.

```bash
# Run these from the root of the repo.
# build
docker run --volume $(pwd):/repo \
    'docker.io/oryxprod/build:latest' \
    oryx build /repo --output /repo

# run

# the -p/--publish and -e/--env flags specify and open a host port
docker run --detach --rm \
    --volume $(pwd):/app \
    --env PORT=8080 \
    --publish 8080:8080 \
    'docker.io/oryxprod/node-10.14:latest' \
    sh -c 'oryx -appPath /app && /run.sh'
```

# Components

Oryx is a collection of the following components:


## Build script generator

The build script generator is a command line tool (and .NET Core library) that can generate and execute build
scripts for a given source repo.

## Build image

We have a single build image which supports all of the SDKs and their versions. This allows developers to use multiple
languages in their build, e.g. run a Python setup script when building their .NET Core app, or have a TypeScript 
frontend for their Python app. You can take a look at its [Dockerfile](./images/build/Dockerfile) to better understand
its contents.

Note that some layers of this build image come from yet another set of images, which we build independently for
modularization and for faster build times. You can see what are those images and how they are built in their
[build script](./build/build-buildimage-bases.sh).

To help the user select which version they want for each platform, they can use the `benv` script pre-installed
in the build image. For example, `source benv python=3.6 node=8` will make Python 3.6 and the latest supported
version of Node 8 the default ones.

The build image also contains the build script generator, which can be accessed by its alias, `oryx`.

## Startup script generators

These are command line tools, one for each platform, that inspect the output directory of an application and
write a script that can start it. They are written in Go, and are located in
[src/startupscriptgenerator](./src/startupscriptgenerator/).

## Runtime images

We have a set of runtime images, and their Dockerfiles are located in [images/runtime](./images/runtime). Some of the Dockerfiles are generated from a template, also located in this folder, with a corresponding script to turn those
scripts into actual Dockerfiles. Having templates helps us maintain consistency across the Dockerfiles.

There are some exceptions that are not templates, where we have to customize the image. A typical need for
such customization is security, where we have to patch a tool or rely on a different base image than the
official images released for a given platform.

Each runtime image contains the startup script generator for its platform.

# Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md).

# License

MIT, see [LICENSE.md](./LICENSE.md).

# Security

Security issues and bugs should be reported privately, via email, to the
Microsoft Security Response Center (MSRC) at
[secure@microsoft.com](mailto:secure@microsoft.com). You should receive a
response within 24 hours. If for some reason you do not, please follow up via
email to ensure we received your original message. Further information,
including the [MSRC
PGP](https://technet.microsoft.com/en-us/security/dn606155) key, can be found
in the [Security
TechCenter](https://technet.microsoft.com/en-us/security/default).

# Data/Telemetry

When utilized within Azure services, this project collects usage data and
sends it to Microsoft to help improve our products and services. Read
[Microsoft's privacy statement][] to learn more.

[Microsoft's privacy statement]: http://go.microsoft.com/fwlink/?LinkId=521839

This project follows the [Microsoft Open Source Code of Conduct][coc]. For
more information see the [Code of Conduct FAQ][cocfaq]. Contact
[opencode@microsoft.com][cocmail] with questions and comments.

[coc]: https://opensource.microsoft.com/codeofconduct/
[cocfaq]: https://opensource.microsoft.com/codeofconduct/faq/
[cocmail]: mailto:opencode@microsoft.com
