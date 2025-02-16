#pragma once

/*
    Production Version
    Nick Daria (contact@nickdaria.com)
*/

#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <string.h>
#include <time.h>

/// The current version of the struct itself
#define PRODVER_STRUCTVER      1

/// Exactly how many bytes the C# code writes/reads for each field.
#define PRODVER_PRODUCT_LEN            24  // in the byte array (struct has +1 for '\0')
#define PRODVER_METADATA_LEN           15  // in the byte array (struct has +1 for '\0')
#define PRODVER_COMMIT_LEN        7   // in the byte array (struct has +1 for '\0')

/// The total bytes that C# produces/consumes
#define PRODVER_ENCODED_LEN    64

/// @brief Unique character indicating the release channel
typedef enum {
    VERSION_CHANNEL_DEV          = 'd',      //  Non-functional development/bench testing
    VERSION_CHANNEL_INTERNAL     = 'i',      //  Semi-functional internal-only use
    VERSION_CHANNEL_ALPHA        = 'a',      //  Functional testing-ready use
    VERSION_CHANNEL_BETA         = 'b',      //  Reliable (no known issues) unreleased build
    VERSION_CHANNEL_CANDIDATE    = 'c',      //  Candidate for release
    VERSION_CHANNEL_RELEASE      = 'r',      //  Production build
    
    VERSION_CHANNEL_FACTORY      = 'f',      //  Factory build (non functional test/updater software)
} prodVersionChannel_t;

typedef struct {
    //  Product/Part Identifier
    char product[PRODVER_PRODUCT_LEN + 1];

    //  Semantic Versioning
    uint16_t major;
    uint16_t minor;
    uint16_t patch;
    
    /// @brief Incrementing build number that resets to 0 on any semantic version increment
    uint16_t build;

    /// @brief Character that indicates the release channel this version targets
    prodVersionChannel_t releaseChannel;

    /// @brief Optional tag appeded to version for part numbers or special variations
    /// @example 1.0.1a-stripped, 1.0.1r-5CW3C
    /// @note 15 characters, last byte reserved for null terminator
    char metadata[PRODVER_METADATA_LEN + 1];

    /// @brief First 7 characters of a Git commit or TFS check-in/shelveset number. If there is no associated commit OR the code has varied at all, make this empty.
    /// @example 7b5a2fe
    /// @note 7 characters, last byte reserved for null terminator
    char commitHash[PRODVER_COMMIT_LEN + 1];

    /// @brief Date & time of software build/finalization
    uint64_t date;
} prodVersion_t;

/// @brief Encodes a version structure into a fixed 64-byte array, matching the C# library.
/// @param ret_buf Destination buffer (must be at least 64 bytes).
/// @param len Length of ret_buf.
/// @param version Pointer to struct to encode.
/// @return Number of bytes written (64) or 0 on error.
static inline size_t prodVersionEncodeBytes(char* ret_buf, const size_t len, const prodVersion_t* version)
{
    if (!ret_buf || !version || len < PRODVER_ENCODED_LEN) {
        return 0;
    }

    size_t offset = 0;

    //  Structure version
    ret_buf[offset++] = PRODVER_STRUCTVER;

    //  Product/Part identifier
    memset(ret_buf + offset, 0, PRODVER_PRODUCT_LEN);
    strncpy(ret_buf + offset, version->product, PRODVER_PRODUCT_LEN);
    offset += PRODVER_PRODUCT_LEN;

    //  Semantic versioning
    ret_buf[offset++] = (char)((version->major >> 8) & 0xFF);
    ret_buf[offset++] = (char)( version->major       & 0xFF);

    ret_buf[offset++] = (char)((version->minor >> 8) & 0xFF);
    ret_buf[offset++] = (char)( version->minor       & 0xFF);

    ret_buf[offset++] = (char)((version->patch >> 8) & 0xFF);
    ret_buf[offset++] = (char)( version->patch       & 0xFF);

    ret_buf[offset++] = (char)((version->build >> 8) & 0xFF);
    ret_buf[offset++] = (char)( version->build       & 0xFF);

    //  Release channel
    ret_buf[offset++] = (char)version->releaseChannel;

    //  Metadata
    memset(ret_buf + offset, 0, PRODVER_METADATA_LEN);
    strncpy(ret_buf + offset, version->metadata, PRODVER_METADATA_LEN);
    offset += PRODVER_METADATA_LEN;

    //  Commit identifier
    memset(ret_buf + offset, 0, PRODVER_COMMIT_LEN);
    strncpy(ret_buf + offset, version->commitHash, PRODVER_COMMIT_LEN);
    offset += PRODVER_COMMIT_LEN;

    //  Date
    uint64_t d = version->date;
    ret_buf[offset++] = (char)((d >> 56) & 0xFF);
    ret_buf[offset++] = (char)((d >> 48) & 0xFF);
    ret_buf[offset++] = (char)((d >> 40) & 0xFF);
    ret_buf[offset++] = (char)((d >> 32) & 0xFF);
    ret_buf[offset++] = (char)((d >> 24) & 0xFF);
    ret_buf[offset++] = (char)((d >> 16) & 0xFF);
    ret_buf[offset++] = (char)((d >>  8) & 0xFF);
    ret_buf[offset++] = (char)( d        & 0xFF);

    return offset;
}

/// @brief Decodes a 64-byte array into a version struct, matching the C# library.
/// @param buf Source data (must be at least 64 bytes).
/// @param len Length of buf.
/// @param ret_version Destination struct.
/// @return True on success, false on error or bad version.
static inline bool prodVersionDecodeBytes(const char* buf, const size_t len, prodVersion_t* ret_version)
{
    if (!buf || !ret_version || len < PRODVER_ENCODED_LEN) {
        return false;
    }

    size_t offset = 0;

    //  Validate structure version
    uint8_t structVer = (uint8_t)buf[offset++];
    if (structVer != PRODVER_STRUCTVER) {
        return false;
    }

    //  Product
    memcpy(ret_version->product, buf + offset, PRODVER_PRODUCT_LEN);
    ret_version->product[PRODVER_PRODUCT_LEN] = '\0'; // safe in struct
    offset += PRODVER_PRODUCT_LEN;

    //  Semantic Versioning
    ret_version->major = (uint16_t)(((uint8_t)buf[offset] << 8) | (uint8_t)buf[offset + 1]);
    offset += 2;
    ret_version->minor = (uint16_t)(((uint8_t)buf[offset] << 8) | (uint8_t)buf[offset + 1]);
    offset += 2;
    ret_version->patch = (uint16_t)(((uint8_t)buf[offset] << 8) | (uint8_t)buf[offset + 1]);
    offset += 2;
    ret_version->build = (uint16_t)(((uint8_t)buf[offset] << 8) | (uint8_t)buf[offset + 1]);
    offset += 2;

    //  Release channel
    ret_version->releaseChannel = (prodVersionChannel_t)buf[offset++];

    //  Metadata
    memcpy(ret_version->metadata, buf + offset, PRODVER_METADATA_LEN);
    ret_version->metadata[PRODVER_METADATA_LEN] = '\0';
    offset += PRODVER_METADATA_LEN;

    //  Commit hash
    memcpy(ret_version->commitHash, buf + offset, PRODVER_COMMIT_LEN);
    ret_version->commitHash[PRODVER_COMMIT_LEN] = '\0';
    offset += PRODVER_COMMIT_LEN;

    //  Date
    uint64_t d = 0;
    d |= (uint64_t)((uint8_t)buf[offset++]) << 56;
    d |= (uint64_t)((uint8_t)buf[offset++]) << 48;
    d |= (uint64_t)((uint8_t)buf[offset++]) << 40;
    d |= (uint64_t)((uint8_t)buf[offset++]) << 32;
    d |= (uint64_t)((uint8_t)buf[offset++]) << 24;
    d |= (uint64_t)((uint8_t)buf[offset++]) << 16;
    d |= (uint64_t)((uint8_t)buf[offset++]) <<  8;
    d |= (uint64_t)((uint8_t)buf[offset++]);
    ret_version->date = d;

    return true;
}

/// @brief Converts a version to a human-readable string.
/// @param version Pointer to version struct
/// @param ret_str Buffer to write string to
/// @param buf_len Length of buffer
/// @return Length of written data, or 0 if buffer is too small
static inline size_t prodVersionToString(const prodVersion_t* version, char* ret_str, size_t buf_len)
{
    if (!version || !ret_str || buf_len == 0) {
        return 0;
    }

    if (buf_len < 2) {
        ret_str[0] = '\0';
        return 0;
    }

    // Example: "MYPRODUCT 1.2.3a-something (abc1234) build 42"
    int written = snprintf(
        ret_str,
        buf_len,
        "%s %u.%u.%u%c%s%s%s%s%s%c",
        version->product[0] ? version->product : "",
        version->major,
        version->minor,
        version->patch,
        (char)version->releaseChannel,
        version->metadata[0] ? "-" : "",
        version->metadata,
        (version->releaseChannel != VERSION_CHANNEL_RELEASE)
            ? " (" : "",
        (version->releaseChannel != VERSION_CHANNEL_RELEASE)
            ? version->commitHash : "",
        (version->releaseChannel != VERSION_CHANNEL_RELEASE)
            ? ")" : "",
        version->build
    );

    if (written < 0 || (size_t)written >= buf_len) {
        ret_str[0] = '\0';
        return 0;
    }

    return (size_t)written;
}