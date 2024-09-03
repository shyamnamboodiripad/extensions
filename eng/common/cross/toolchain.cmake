set(CROSS_ROOTFS $ENV{ROOTFS_DIR})

# reset platform variables (e.g. cmake 3.25 sets LINUX=1)
unset(LINUX)
unset(FREEBSD)
unset(ILLUMOS)
unset(ANDROID)
unset(TIZEN)
unset(HAIKU)

set(TARGET_ARCH_NAME $ENV{TARGET_BUILD_ARCH})
if(EXISTS ${CROSS_ROOTFS}/bin/freebsd-version)
  set(CMAKE_SYSTEM_NAME FreeBSD)
  set(FREEBSD 1)
elseif(EXISTS ${CROSS_ROOTFS}/usr/platform/i86pc)
  set(CMAKE_SYSTEM_NAME SunOS)
  set(ILLUMOS 1)
elseif(EXISTS ${CROSS_ROOTFS}/boot/system/develop/headers/config/HaikuConfig.h)
  set(CMAKE_SYSTEM_NAME Haiku)
  set(HAIKU 1)
else()
  set(CMAKE_SYSTEM_NAME Linux)
  set(LINUX 1)
endif()
set(CMAKE_SYSTEM_VERSION 1)

if(EXISTS ${CROSS_ROOTFS}/etc/tizen-release)
  set(TIZEN 1)
elseif(EXISTS ${CROSS_ROOTFS}/android_platform)
  set(ANDROID 1)
endif()

if(TARGET_ARCH_NAME STREQUAL "arm")
  set(CMAKE_SYSTEM_PROCESSOR armv7l)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/armv7-alpine-linux-musleabihf)
    set(TOOLCHAIN "armv7-alpine-linux-musleabihf")
  elseif(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/armv6-alpine-linux-musleabihf)
    set(TOOLCHAIN "armv6-alpine-linux-musleabihf")
  else()
    set(TOOLCHAIN "arm-linux-gnueabihf")
  endif()
  if(TIZEN)
    set(TIZEN_TOOLCHAIN "armv7hl-tizen-linux-gnueabihf/9.2.0")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "arm64")
  set(CMAKE_SYSTEM_PROCESSOR aarch64)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/aarch64-alpine-linux-musl)
    set(TOOLCHAIN "aarch64-alpine-linux-musl")
  elseif(LINUX)
    set(TOOLCHAIN "aarch64-linux-gnu")
    if(TIZEN)
      set(TIZEN_TOOLCHAIN "aarch64-tizen-linux-gnu/9.2.0")
    endif()
  elseif(FREEBSD)
    set(triple "aarch64-unknown-freebsd12")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "armel")
  set(CMAKE_SYSTEM_PROCESSOR armv7l)
  set(TOOLCHAIN "arm-linux-gnueabi")
  if(TIZEN)
    set(TIZEN_TOOLCHAIN "armv7l-tizen-linux-gnueabi/9.2.0")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "armv6")
  set(CMAKE_SYSTEM_PROCESSOR armv6l)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/armv6-alpine-linux-musleabihf)
    set(TOOLCHAIN "armv6-alpine-linux-musleabihf")
  else()
    set(TOOLCHAIN "arm-linux-gnueabihf")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "ppc64le")
  set(CMAKE_SYSTEM_PROCESSOR ppc64le)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/powerpc64le-alpine-linux-musl)
    set(TOOLCHAIN "powerpc64le-alpine-linux-musl")
  else()
    set(TOOLCHAIN "powerpc64le-linux-gnu")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "riscv64")
  set(CMAKE_SYSTEM_PROCESSOR riscv64)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/riscv64-alpine-linux-musl)
    set(TOOLCHAIN "riscv64-alpine-linux-musl")
  else()
    set(TOOLCHAIN "riscv64-linux-gnu")
    if(TIZEN)
      set(TIZEN_TOOLCHAIN "riscv64-tizen-linux-gnu/13.1.0")
    endif()
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "s390x")
  set(CMAKE_SYSTEM_PROCESSOR s390x)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/s390x-alpine-linux-musl)
    set(TOOLCHAIN "s390x-alpine-linux-musl")
  else()
    set(TOOLCHAIN "s390x-linux-gnu")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "x64")
  set(CMAKE_SYSTEM_PROCESSOR x86_64)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/x86_64-alpine-linux-musl)
    set(TOOLCHAIN "x86_64-alpine-linux-musl")
  elseif(LINUX)
    set(TOOLCHAIN "x86_64-linux-gnu")
    if(TIZEN)
      set(TIZEN_TOOLCHAIN "x86_64-tizen-linux-gnu/9.2.0")
    endif()
  elseif(FREEBSD)
    set(triple "x86_64-unknown-freebsd12")
  elseif(ILLUMOS)
    set(TOOLCHAIN "x86_64-illumos")
  elseif(HAIKU)
    set(TOOLCHAIN "x86_64-unknown-haiku")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "x86")
  set(CMAKE_SYSTEM_PROCESSOR i686)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/i586-alpine-linux-musl)
    set(TOOLCHAIN "i586-alpine-linux-musl")
  else()
    set(TOOLCHAIN "i686-linux-gnu")
  endif()
  if(TIZEN)
    set(TIZEN_TOOLCHAIN "i586-tizen-linux-gnu/9.2.0")
  endif()
else()
  message(FATAL_ERROR "Arch is ${TARGET_ARCH_NAME}. Only arm, arm64, armel, armv6, ppc64le, riscv64, s390x, x64 and x86 are supported!")
endif()

if(DEFINED ENV{TOOLCHAIN})
  set(TOOLCHAIN $ENV{TOOLCHAIN})
endif()

# Specify include paths
if(TIZEN)
  if(TARGET_ARCH_NAME STREQUAL "arm")
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/armv7hl-tizen-linux-gnueabihf)
  endif()
  if(TARGET_ARCH_NAME STREQUAL "armel")
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/armv7l-tizen-linux-gnueabi)
  endif()
  if(TARGET_ARCH_NAME STREQUAL "arm64")
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}/include/c++/)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}/include/c++/aarch64-tizen-linux-gnu)
  endif()
  if(TARGET_ARCH_NAME STREQUAL "x86")
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/i586-tizen-linux-gnu)
  endif()
  if(TARGET_ARCH_NAME STREQUAL "x64")
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}/include/c++/)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}/include/c++/x86_64-tizen-linux-gnu)
  endif()
  if(TARGET_ARCH_NAME STREQUAL "riscv64")
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}/include/c++/)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}/include/c++/riscv64-tizen-linux-gnu)
  endif()
endif()

if(ANDROID)
    if(TARGET_ARCH_NAME STREQUAL "arm")
        set(ANDROID_ABI armeabi-v7a)
    elseif(TARGET_ARCH_NAME STREQUAL "arm64")
        set(ANDROID_ABI arm64-v8a)
    endif()

    # extract platform number required by the NDK's toolchain
    file(READ "${CROSS_ROOTFS}/android_platform" RID_FILE_CONTENTS)
    string(REPLACE "RID=" "" ANDROID_RID "${RID_FILE_CONTENTS}")
    string(REGEX REPLACE ".*\\.([0-9]+)-.*" "\\1" ANDROID_PLATFORM "${ANDROID_RID}")

    set(ANDROID_TOOLCHAIN clang)
    set(FEATURE_EVENT_TRACE 0) # disable event trace as there is no lttng-ust package in termux repository
    set(CMAKE_SYSTEM_LIBRARY_PATH "${CROSS_ROOTFS}/usr/lib")
    set(CMAKE_SYSTEM_INCLUDE_PATH "${CROSS_ROOTFS}/usr/include")

    # include official NDK toolchain script
    include(${CROSS_ROOTFS}/../build/cmake/android.toolchain.cmake)
elseif(FREEBSD)
    # we cross-compile by instructing clang
    set(CMAKE_C_COMPILER_TARGET ${triple})
    set(CMAKE_CXX_COMPILER_TARGET ${triple})
    set(CMAKE_ASM_COMPILER_TARGET ${triple})
    set(CMAKE_SYSROOT "${CROSS_ROOTFS}")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -fuse-ld=lld")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -fuse-ld=lld")
    set(CMAKE_MODULE_LINKER_FLAGS "${CMAKE_MODULE_LINKER_FLAGS} -fuse-ld=lld")
elseif(ILLUMOS)
    set(CMAKE_SYSROOT "${CROSS_ROOTFS}")

    include_directories(SYSTEM ${CROSS_ROOTFS}/include)

    set(TOOLSET_PREFIX ${TOOLCHAIN}-)
    function(locate_toolchain_exec exec var)
        string(TOUPPER ${exec} EXEC_UPPERCASE)
        if(NOT "$ENV{CLR_${EXEC_UPPERCASE}}" STREQUAL "")
            set(${var} "$ENV{CLR_${EXEC_UPPERCASE}}" PARENT_SCOPE)
            return()
        endif()

        find_program(EXEC_LOCATION_${exec}
            NAMES
            "${TOOLSET_PREFIX}${exec}${CLR_CMAKE_COMPILER_FILE_NAME_VERSION}"
            "${TOOLSET_PREFIX}${exec}")

        if (EXEC_LOCATION_${exec} STREQUAL "EXEC_LOCATION_${exec}-NOTFOUND")
            message(FATAL_ERROR "Unable to find toolchain executable. Name: ${exec}, Prefix: ${TOOLSET_PREFIX}.")
        endif()
        set(${var} ${EXEC_LOCATION_${exec}} PARENT_SCOPE)
    endfunction()

    set(CMAKE_SYSTEM_PREFIX_PATH "${CROSS_ROOTFS}")

    locate_toolchain_exec(gcc CMAKE_C_COMPILER)
    locate_toolchain_exec(g++ CMAKE_CXX_COMPILER)

    set(CMAKE_C_STANDARD_LIBRARIES "${CMAKE_C_STANDARD_LIBRARIES} -lssp")
    set(CMAKE_CXX_STANDARD_LIBRARIES "${CMAKE_CXX_STANDARD_LIBRARIES} -lssp")
elseif(HAIKU)
    set(CMAKE_SYSROOT "${CROSS_ROOTFS}")
    set(CMAKE_PROGRAM_PATH "${CMAKE_PROGRAM_PATH};${CROSS_ROOTFS}/cross-tools-x86_64/bin")

    set(TOOLSET_PREFIX ${TOOLCHAIN}-)
    function(locate_toolchain_exec exec var)
        string(TOUPPER ${exec} EXEC_UPPERCASE)
        if(NOT "$ENV{CLR_${EXEC_UPPERCASE}}" STREQUAL "")
            set(${var} "$ENV{CLR_${EXEC_UPPERCASE}}" PARENT_SCOPE)
            return()
        endif()

        find_program(EXEC_LOCATION_${exec}
            NAMES
            "${TOOLSET_PREFIX}${exec}${CLR_CMAKE_COMPILER_FILE_NAME_VERSION}"
            "${TOOLSET_PREFIX}${exec}")

        if (EXEC_LOCATION_${exec} STREQUAL "EXEC_LOCATION_${exec}-NOTFOUND")
            message(FATAL_ERROR "Unable to find toolchain executable. Name: ${exec}, Prefix: ${TOOLSET_PREFIX}.")
        endif()
        set(${var} ${EXEC_LOCATION_${exec}} PARENT_SCOPE)
    endfunction()

    set(CMAKE_SYSTEM_PREFIX_PATH "${CROSS_ROOTFS}")

    locate_toolchain_exec(gcc CMAKE_C_COMPILER)
    locate_toolchain_exec(g++ CMAKE_CXX_COMPILER)

    set(CMAKE_C_STANDARD_LIBRARIES "${CMAKE_C_STANDARD_LIBRARIES} -lssp")
    set(CMAKE_CXX_STANDARD_LIBRARIES "${CMAKE_CXX_STANDARD_LIBRARIES} -lssp")

    # let CMake set up the correct search paths
    include(Platform/Haiku)
else()
    set(CMAKE_SYSROOT "${CROSS_ROOTFS}")

    set(CMAKE_C_COMPILER_EXTERNAL_TOOLCHAIN "${CROSS_ROOTFS}/usr")
    set(CMAKE_CXX_COMPILER_EXTERNAL_TOOLCHAIN "${CROSS_ROOTFS}/usr")
    set(CMAKE_ASM_COMPILER_EXTERNAL_TOOLCHAIN "${CROSS_ROOTFS}/usr")
endif()

# Specify link flags

function(add_toolchain_linker_flag Flag)
  set(Config "${ARGV1}")
  set(CONFIG_SUFFIX "")
  if (NOT Config STREQUAL "")
    set(CONFIG_SUFFIX "_${Config}")
  endif()
  set("CMAKE_EXE_LINKER_FLAGS${CONFIG_SUFFIX}_INIT" "${CMAKE_EXE_LINKER_FLAGS${CONFIG_SUFFIX}_INIT} ${Flag}" PARENT_SCOPE)
  set("CMAKE_SHARED_LINKER_FLAGS${CONFIG_SUFFIX}_INIT" "${CMAKE_SHARED_LINKER_FLAGS${CONFIG_SUFFIX}_INIT} ${Flag}" PARENT_SCOPE)
endfunction()

if(LINUX)
  add_toolchain_linker_flag("-Wl,--rpath-link=${CROSS_ROOTFS}/lib/${TOOLCHAIN}")
  add_toolchain_linker_flag("-Wl,--rpath-link=${CROSS_ROOTFS}/usr/lib/${TOOLCHAIN}")
endif()

if(TARGET_ARCH_NAME MATCHES "^(arm|armel)$")
  if(TIZEN)
    add_toolchain_linker_flag("-B${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/lib")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/usr/lib")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
  endif()
elseif(TARGET_ARCH_NAME MATCHES "^(arm64|x64|riscv64)$")
  if(TIZEN)
    add_toolchain_linker_flag("-B${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/lib64")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/usr/lib64")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}")

    add_toolchain_linker_flag("-Wl,--rpath-link=${CROSS_ROOTFS}/lib64")
    add_toolchain_linker_flag("-Wl,--rpath-link=${CROSS_ROOTFS}/usr/lib64")
    add_toolchain_linker_flag("-Wl,--rpath-link=${CROSS_ROOTFS}/usr/lib64/gcc/${TIZEN_TOOLCHAIN}")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "s390x")
  add_toolchain_linker_flag("--target=${TOOLCHAIN}")
elseif(TARGET_ARCH_NAME STREQUAL "x86")
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/i586-alpine-linux-musl)
    add_toolchain_linker_flag("--target=${TOOLCHAIN}")
    add_toolchain_linker_flag("-Wl,--rpath-link=${CROSS_ROOTFS}/usr/lib/gcc/${TOOLCHAIN}")
  endif()
  add_toolchain_linker_flag(-m32)
  if(TIZEN)
    add_toolchain_linker_flag("-B${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/lib")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/usr/lib")
    add_toolchain_linker_flag("-L${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
  endif()
elseif(ILLUMOS)
  add_toolchain_linker_flag("-L${CROSS_ROOTFS}/lib/amd64")
  add_toolchain_linker_flag("-L${CROSS_ROOTFS}/usr/amd64/lib")
elseif(HAIKU)
  add_toolchain_linker_flag("-lnetwork")
  add_toolchain_linker_flag("-lroot")
endif()

# Specify compile options

if((TARGET_ARCH_NAME MATCHES "^(arm|arm64|armel|armv6|ppc64le|riscv64|s390x|x64|x86)$" AND NOT ANDROID AND NOT FREEBSD) OR ILLUMOS OR HAIKU)
  set(CMAKE_C_COMPILER_TARGET ${TOOLCHAIN})
  set(CMAKE_CXX_COMPILER_TARGET ${TOOLCHAIN})
  set(CMAKE_ASM_COMPILER_TARGET ${TOOLCHAIN})
endif()

if(TARGET_ARCH_NAME MATCHES "^(arm|armel)$")
  add_compile_options(-mthumb)
  if (NOT DEFINED CLR_ARM_FPU_TYPE)
    set (CLR_ARM_FPU_TYPE vfpv3)
  endif (NOT DEFINED CLR_ARM_FPU_TYPE)

  add_compile_options (-mfpu=${CLR_ARM_FPU_TYPE})
  if (NOT DEFINED CLR_ARM_FPU_CAPABILITY)
    set (CLR_ARM_FPU_CAPABILITY 0x7)
  endif (NOT DEFINED CLR_ARM_FPU_CAPABILITY)

  add_definitions (-DCLR_ARM_FPU_CAPABILITY=${CLR_ARM_FPU_CAPABILITY})

  # persist variables across multiple try_compile passes
  list(APPEND CMAKE_TRY_COMPILE_PLATFORM_VARIABLES CLR_ARM_FPU_TYPE CLR_ARM_FPU_CAPABILITY)

  if(TARGET_ARCH_NAME STREQUAL "armel")
    add_compile_options(-mfloat-abi=softfp)
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "s390x")
  add_compile_options("--target=${TOOLCHAIN}")
elseif(TARGET_ARCH_NAME STREQUAL "x86")
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/i586-alpine-linux-musl)
    add_compile_options(--target=${TOOLCHAIN})
  endif()
  add_compile_options(-m32)
  add_compile_options(-Wno-error=unused-command-line-argument)
endif()

if(TIZEN)
  if(TARGET_ARCH_NAME MATCHES "^(arm|armel|arm64|x86)$")
    add_compile_options(-Wno-deprecated-declarations) # compile-time option
    add_compile_options(-D__extern_always_inline=inline) # compile-time option
  endif()
endif()

# Set LLDB include and library paths for builds that need lldb.
if(TARGET_ARCH_NAME MATCHES "^(arm|armel|x86)$")
  if(TARGET_ARCH_NAME STREQUAL "x86")
    set(LLVM_CROSS_DIR "$ENV{LLVM_CROSS_HOME}")
  else() # arm/armel case
    set(LLVM_CROSS_DIR "$ENV{LLVM_ARM_HOME}")
  endif()
  if(LLVM_CROSS_DIR)
    set(WITH_LLDB_LIBS "${LLVM_CROSS_DIR}/lib/" CACHE STRING "")
    set(WITH_LLDB_INCLUDES "${LLVM_CROSS_DIR}/include" CACHE STRING "")
    set(LLDB_H "${WITH_LLDB_INCLUDES}" CACHE STRING "")
    set(LLDB "${LLVM_CROSS_DIR}/lib/liblldb.so" CACHE STRING "")
  else()
    if(TARGET_ARCH_NAME STREQUAL "x86")
      set(WITH_LLDB_LIBS "${CROSS_ROOTFS}/usr/lib/i386-linux-gnu" CACHE STRING "")
      set(CHECK_LLVM_DIR "${CROSS_ROOTFS}/usr/lib/llvm-3.8/include")
      if(EXISTS "${CHECK_LLVM_DIR}" AND IS_DIRECTORY "${CHECK_LLVM_DIR}")
        set(WITH_LLDB_INCLUDES "${CHECK_LLVM_DIR}")
      else()
        set(WITH_LLDB_INCLUDES "${CROSS_ROOTFS}/usr/lib/llvm-3.6/include")
      endif()
    else() # arm/armel case
      set(WITH_LLDB_LIBS "${CROSS_ROOTFS}/usr/lib/${TOOLCHAIN}" CACHE STRING "")
      set(WITH_LLDB_INCLUDES "${CROSS_ROOTFS}/usr/lib/llvm-3.6/include" CACHE STRING "")
    endif()
  endif()
endif()

# Set C++ standard library options if specified
set(CLR_CMAKE_CXX_STANDARD_LIBRARY "" CACHE STRING "Standard library flavor to link against. Only supported with the Clang compiler.")
if (CLR_CMAKE_CXX_STANDARD_LIBRARY)
  add_compile_options($<$<COMPILE_LANG_AND_ID:CXX,Clang>:--stdlib=${CLR_CMAKE_CXX_STANDARD_LIBRARY}>)
  add_link_options($<$<LINK_LANG_AND_ID:CXX,Clang>:--stdlib=${CLR_CMAKE_CXX_STANDARD_LIBRARY}>)
endif()

option(CLR_CMAKE_CXX_STANDARD_LIBRARY_STATIC "Statically link against the C++ standard library" OFF)
if(CLR_CMAKE_CXX_STANDARD_LIBRARY_STATIC)
  add_link_options($<$<LINK_LANGUAGE:CXX>:-static-libstdc++>)
endif()

set(CLR_CMAKE_CXX_ABI_LIBRARY "" CACHE STRING "C++ ABI implementation library to link against. Only supported with the Clang compiler.")
if (CLR_CMAKE_CXX_ABI_LIBRARY)
  # The user may specify the ABI library with the 'lib' prefix, like 'libstdc++'. Strip the prefix here so the linker finds the right library.
  string(REGEX REPLACE "^lib(.+)" "\\1" CLR_CMAKE_CXX_ABI_LIBRARY ${CLR_CMAKE_CXX_ABI_LIBRARY})
  # We need to specify this as a linker-backend option as Clang will filter this option out when linking to libc++.
  add_link_options("LINKER:-l${CLR_CMAKE_CXX_ABI_LIBRARY}")
endif()

set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
