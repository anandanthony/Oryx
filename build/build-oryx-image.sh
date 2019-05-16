#!/bin/bash
# --------------------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.
# --------------------------------------------------------------------------------------------

set -e

declare -r REPO_DIR=$( cd $( dirname "$0" ) && cd .. && pwd )
declare -r buildRuntimeImagesScript="$REPO_DIR/build/build-runtimeimages.sh"
declare -r buildBuildImagesScript="$REPO_DIR/build/build-buildimages.sh"

# Load all variables
source $REPO_DIR/build/__variables.sh

echo $BUILD_IMAGES_BUILD_CONTEXT_DIR

cd "$BUILD_IMAGES_BUILD_CONTEXT_DIR"

if [ -n "$1" ]
then
	case $1 in 
		build)
			echo "Oryx build image will be built..."
			$buildBuildImagesScript
			;;
		node)
			echo "Oryx runtime node images will be built... "
			$buildRuntimeImagesScript "$1"
			;;
		*)
			echo "Wrong or no image selected..."
			;;
	esac
fi