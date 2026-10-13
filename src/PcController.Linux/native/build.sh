#!/bin/sh
set -eu
# System libraries supplied/updated by Ubuntu; no privileged input dependency.
test_flag=
if [ "${3:-}" = test ]; then test_flag=-DPC_REMOTE_TESTING; fi
exec cc -std=c11 -D_GNU_SOURCE -fPIC -shared -O2 -Wall -Wextra -Werror \
  $test_flag "$1" -o "$2" $(pkg-config --cflags --libs gio-2.0 gio-unix-2.0 libpulse libsecret-1 xcb xcb-xtest xkbcommon-x11) -pthread -lm
