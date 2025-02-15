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
    char product[25+1];

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
    char metadata[16+1];

    /// @brief First 7 characters of a Git commit or TFS check-in/shelveset number. If there is no associated commit OR the code has varied at all, make this empty.
    /// @example 7b5a2fe
    /// @note 7 characters, last byte reserved for null terminator
    char commitHash[7+1];

    /// @brief Date & time of software build/finalization
    uint64_t date;
} prodVersion_t;

/// @brief Encodes a version structure into a byte array
/// @param ret_buf Buffer to write encoded data to
/// @param len Length of buffer available
/// @param version Version struct
/// @return Length of data written (or 0 if buffer is insufficient)
static inline size_t prodVersionEncodeBytes(char* ret_buf, const size_t len, const prodVersion_t* version) {
    if (!ret_buf || !version || len < sizeof(prodVersion_t)) return 0;

    size_t offset = 0;

    memcpy(ret_buf + offset, &version->major, sizeof(version->major));
    offset += sizeof(version->major);
    
    memcpy(ret_buf + offset, &version->minor, sizeof(version->minor));
    offset += sizeof(version->minor);

    memcpy(ret_buf + offset, &version->patch, sizeof(version->patch));
    offset += sizeof(version->patch);

    memcpy(ret_buf + offset, &version->build, sizeof(version->build));
    offset += sizeof(version->build);

    ret_buf[offset++] = (char)version->releaseChannel;

    // Copy metadata safely
    strncpy(ret_buf + offset, version->metadata, sizeof(version->metadata) - 1);
    ret_buf[offset + sizeof(version->metadata) - 1] = '\0'; // Ensure null termination
    offset += sizeof(version->metadata);

    // Copy commit hash safely
    strncpy(ret_buf + offset, version->commitHash, sizeof(version->commitHash) - 1);
    ret_buf[offset + sizeof(version->commitHash) - 1] = '\0'; // Ensure null termination
    offset += sizeof(version->commitHash);

    memcpy(ret_buf + offset, &version->date, sizeof(version->date));
    offset += sizeof(version->date);

    return offset;
}

/// @brief Decodes a version struct from a byte array
/// @param buf Pointer to buffer data
/// @param len Length of buffer data
/// @param version Pointer to return struct
/// @return True if decoding succeeds, false otherwise
static inline bool prodVersionDecodeBytes(const char* buf, const size_t len, prodVersion_t* ret_version) {
    if (!buf || !ret_version || len < sizeof(prodVersion_t)) return false;

    size_t offset = 0;

    memcpy(&ret_version->major, buf + offset, sizeof(ret_version->major));
    offset += sizeof(ret_version->major);

    memcpy(&ret_version->minor, buf + offset, sizeof(ret_version->minor));
    offset += sizeof(ret_version->minor);

    memcpy(&ret_version->patch, buf + offset, sizeof(ret_version->patch));
    offset += sizeof(ret_version->patch);

    memcpy(&ret_version->build, buf + offset, sizeof(ret_version->build));
    offset += sizeof(ret_version->build);

    ret_version->releaseChannel = (prodVersionChannel_t)buf[offset++];

    // Copy metadata safely
    strncpy(ret_version->metadata, buf + offset, sizeof(ret_version->metadata) - 1);
    ret_version->metadata[sizeof(ret_version->metadata) - 1] = '\0';
    offset += sizeof(ret_version->metadata);

    // Copy commit hash safely
    strncpy(ret_version->commitHash, buf + offset, sizeof(ret_version->commitHash) - 1);
    ret_version->commitHash[sizeof(ret_version->commitHash) - 1] = '\0';
    offset += sizeof(ret_version->commitHash);
    
    memcpy(&ret_version->date, buf + offset, sizeof(ret_version->date));
    offset += sizeof(ret_version->date);

    return true;
}

/// @brief Converts a version to a human-readable string - PRODUCT MAJOR.MINOR.PATCHc-METADATA bBUILD (COMMIT)
/// @example ND-PRODVER 1.2.3a-stripped b37 f32u8a2
/// @param version Version struct
/// @param ret_str Buffer to write string to
/// @param buf_len Length of buffer
/// @return Length of written data, or 0 if buffer is too small
static inline size_t prodVersionToString(const prodVersion_t* version, char* ret_str, size_t buf_len) {
    if (!version || !ret_str || buf_len == 0) return 0;

    if (buf_len < 2) {
        ret_str[0] = '\0';
        return 0;
    }

    int written = snprintf(ret_str, buf_len, "%s %u.%u.%u%c%s%s%s%s",
        version->product[0] ? version->product : "",
        version->major, version->minor, version->patch, (char)version->releaseChannel,
        version->metadata[0] ? "-" : "", version->metadata,
        (version->releaseChannel != VERSION_CHANNEL_RELEASE) ? " " : "",
        (version->releaseChannel != VERSION_CHANNEL_RELEASE) ? version->commitHash : ""
    );

    if (written < 0 || (size_t)written >= buf_len) {
        ret_str[0] = '\0';
        return 0;
    }

    return (size_t)written;
}