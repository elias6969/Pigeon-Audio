
#ifndef FILEMANAGER_H
#define FILEMANAGER_H

#include <filesystem>
#include <string>

namespace fs = std::filesystem;

class VirtualFileSystem {
public:
  VirtualFileSystem();
  explicit VirtualFileSystem(const std::string &basePath);
  std::string getFullPath(const std::string &relativePath) const;
  std::string readFile(const std::string &relativePath);

private:
  std::string baseDir;
};

#endif // FILEMANAGER_H
