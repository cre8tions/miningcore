#/usr/bin/env bash

cd ./secp256k1

./autogen.sh
./configure
make -j