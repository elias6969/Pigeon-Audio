#include "Camera.h"
#include "Shader.h"
#include "audio.h"
#include "filemanager.h"
#include <GLFW/glfw3.h>
#include <cmath>
#include <filesystem>
#include <glad/glad.h>
#include <iostream>
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"
#include "tinyfiledialogs.h"
#include <backends/imgui_impl_glfw.h>
#include <backends/imgui_impl_opengl3.h>
#include <imgui.h>
#include <imguiThemes.h>

void framebuffer_size_callback(GLFWwindow *window, int width, int height);
void processInput(GLFWwindow *window);

// settings
int SCR_WIDTH = 800;
int SCR_HEIGHT = 600;

int main() {
  glfwInit();
  glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 4);
  glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 2);
  glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
  glfwWindowHint(GLFW_ALPHA_BITS, 8);
  glfwWindowHint(GLFW_TRANSPARENT_FRAMEBUFFER, GLFW_TRUE);

#ifdef __APPLE__
  glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);
#endif

  GLFWwindow *window =
      glfwCreateWindow(SCR_WIDTH, SCR_HEIGHT, "LearnOpenGL", NULL, NULL);
  if (window == NULL) {
    std::cout << "Failed to create GLFW window" << std::endl;
    glfwTerminate();
    return -1;
  }
  glfwMakeContextCurrent(window);
  glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);

  if (!gladLoadGLLoader((GLADloadproc)glfwGetProcAddress)) {
    std::cout << "Failed to initialize GLAD" << std::endl;
    return -1;
  }

  // 1) Setup Dear ImGui context
  IMGUI_CHECKVERSION();
  ImGui::CreateContext();
  ImGuiIO &io = ImGui::GetIO();
  (void)io;
  ImGui::StyleColorsDark();

  ImGui_ImplGlfw_InitForOpenGL(window, true);
  ImGui_ImplOpenGL3_Init("#version 420 core");

  AudioPlayer player;
  player.init();
  bool isrender = false;
  const char *filePath;
  char imagePath[256] = "";
  VirtualFileSystem vfs("assets");
  
  // render loop
  while (!glfwWindowShouldClose(window)) {
    processInput(window);
    static double lastTime = glfwGetTime();
    double now = glfwGetTime();
    float dt = float(now - lastTime);
    lastTime = now;

    float amp = get_amplitude();
    float time = glfwGetTime();
    glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
    glClear(GL_COLOR_BUFFER_BIT);

    ImGui_ImplOpenGL3_NewFrame();
    ImGui_ImplGlfw_NewFrame();
    ImGui::NewFrame();
    if (glfwGetKey(window, GLFW_KEY_F) == GLFW_PRESS) {
      isrender = true;
    } else if (glfwGetKey(window, GLFW_KEY_G) == GLFW_PRESS) {
      isrender = false;
    }

    if (isrender) {
      ImGui::Begin("Visualization");
      const char *items[] = {"Simple", "Bars", "glob"};
      ImGui::Combo("Shader Mode", &player.shadermode, items,
                   IM_ARRAYSIZE(items));
      ImGui::Separator();
      ImGui::Text("Image");

      static int selectedImageIndex = player.selectedImage;
      if (!player.textureItems.empty()) {
        if (ImGui::Combo("Images", &selectedImageIndex,
                         player.textureItems.data(),
                         player.textureItems.size())) {
          if (selectedImageIndex != player.selectedImage) {
            player.selectedImage = selectedImageIndex;
            player.loadSelectedTexture();
          }
        }
      }

      ImGui::Text("Load image");
      ImGui::InputText("##Load image", imagePath, IM_ARRAYSIZE(imagePath));
      ImGui::SameLine();
      if (ImGui::Button("Browse")) {
        const char *filter[] = {"*.png", "*.jpg", "*.jpeg", "*.bmp"};
        filePath =
            tinyfd_openFileDialog("Select an Image", // aTitle
                                  "",                // aDefaultPathAndFile
                                  4,                 // aNumOfFilterPatterns
                                  filter,            // aFilterPatterns
                                  "Image Files",     // aSingleFilterDescription
                                  0 // aAllowMultipleSelects (0 = single file)
            );

        if (filePath) {
          strncpy(imagePath, filePath, IM_ARRAYSIZE(imagePath));
        }
      }

      if (ImGui::Button("Load", ImVec2(200, 40))) {
        std::string imgName =
            std::filesystem::path(imagePath).filename().string();

        std::filesystem::path source(imagePath);
        std::filesystem::path destination =
            vfs.getFullPath("Textures/") + source.filename().string();

        try {
          std::filesystem::copy_file(
              source, destination,
              std::filesystem::copy_options::overwrite_existing);
        } catch (const std::filesystem::filesystem_error &e) {
          std::cerr << "Failed to copy file: " << e.what() << '\n';
        }
        player.textureNames.push_back(imgName);
        player.textureItems.push_back(player.textureNames.back().c_str());
        player.selectedImage = player.textureNames.size() - 1;
        player.loadSelectedTexture();
      }
      ImGui::End();
    }

    player.render(&amp, &time, dt, SCR_WIDTH, SCR_HEIGHT);

    ImGui::Render();
    ImGui_ImplOpenGL3_RenderDrawData(ImGui::GetDrawData());

    glfwSwapBuffers(window);
    glfwPollEvents();
  }

  ImGui_ImplOpenGL3_Shutdown();
  ImGui_ImplGlfw_Shutdown();
  ImGui::DestroyContext();
  glfwTerminate();
  return 0;
}

void processInput(GLFWwindow *window) {
  if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
    glfwSetWindowShouldClose(window, true);
}

void framebuffer_size_callback(GLFWwindow *window, int width, int height) {
  glViewport(0, 0, width, height);
  SCR_WIDTH = width;
  SCR_HEIGHT = height;
}
