#include <iostream>
#include <fstream>
#include <stdexcept>
#include "filemanager.h"


VirtualFileSystem::VirtualFileSystem() {
    // Figure out the absolute path to the running executable
    fs::path exePath = fs::canonical("/proc/self/exe");
    // We assume your binary is in build/, so go up one level to project root
    fs::path projectRoot = exePath.parent_path().parent_path();
    baseDir = (projectRoot / "assets").string();

    if (!fs::exists(baseDir)) {
        std::cerr << "Base directory does not exist: "
                  << baseDir << std::endl;
        throw std::runtime_error("Missing assets directory at " + baseDir);
    }
    std::cout << "BASE-DIRECTORY-FOUND::" << baseDir << std::endl;
}

VirtualFileSystem::VirtualFileSystem(const std::string& basePath) : baseDir(basePath) {
    if (!fs::exists(baseDir)) {
        std::cerr << "Base directory does not exist: " << fs::absolute(baseDir) << std::endl;
        throw std::runtime_error("Base directory does not exist: " + baseDir);
    }
    std::cout << "BASE-DIRECTORY-FOUND::" << fs::absolute(baseDir) << std::endl;
}


std::string VirtualFileSystem::getFullPath(const std::string& relativePath) const {
    fs::path fullPath = fs::path(baseDir) / relativePath;
    if (!fs::exists(fullPath)) {
        throw std::runtime_error("File not found: " + fullPath.string());
    }
    // Return the absolute path instead of a relative one.
    return fs::absolute(fullPath).string();
}


std::string VirtualFileSystem::readFile(const std::string& relativePath) {
    std::string fullPath = getFullPath(relativePath);
    std::ifstream file(fullPath);
    if (!file.is_open()) {
        throw std::runtime_error("Could not open file: " + fullPath);
    }
    return std::string((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
}
