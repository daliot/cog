#!/bin/bash -e

PREBUILT_IMAGE_URL="https://ci.lille.inria.fr/pharo/job/Cog%20Git%20Tracker/lastSuccessfulBuild/artifact/vmmaker-image.zip"

NO_COLOR='\033[0m' #disable any colors
YELLOW='\033[0;33m'

WGET_CERTCHECK="--no-check-certificate"
# on macs wget is pretty old and not recognizing this option 
wget --help | grep -- "$WGET_CERTCHECK" 2>&1 > /dev/null || WGET_CERTCHECK=''

# find the location of this script -------------------------------------------
DIR=`readlink "$0"` || DIR="$0";
DIR=`dirname "$DIR"`;
cd "$DIR"
DIR=`pwd`
cd - > /dev/null

# ---------------------------------------------------------------------------

if [ -e $DIR/generator.image ]; then
    echo -e "${YELLOW}VMMaker IMAGE READY TO USE" $NO_COLOR
    echo "    $DIR/generator.image"
    exit 0
fi

# ----------------------------------------------------------------------------
echo -e $YELLOW DOWLOADING THE LATEST VM $NO_COLOR
wget --quiet -qO - http://pharo.gforge.inria.fr/ci/ciCog.sh | bash
# move the downloaded vm in it's proper place
mv vm $DIR/
mv vm.sh $DIR/


# ----------------------------------------------------------------------------
echo -e "${YELLOW}LOADING PREBUILT IMAGE" $NO_COLOR
echo "    $PREBUILT_IMAGE_URL"


wget $WGET_CERTCHECK "$PREBUILT_IMAGE_URL" --output-document="$DIR/image.zip"
unzip $DIR/image.zip -d $DIR
rm $DIR/image.zip
$DIR/vm.sh "$DIR/generator.image" "$DIR/../codegen-scripts/ImageConfiguration.st"

echo -e "${YELLOW}VMMaker IMAGE READY TO USE" $NO_COLOR
echo "    $DIR/generator.image"

