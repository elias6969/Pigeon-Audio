#ifndef AUDIO_H
#define AUDIO_H
#include <portaudio.h>
#include <fftw3.h>
#include <vector>
#include <mutex>
#include <cmath>
#include <iostream>
#include "Camera.h"
#include "Shader.h"

void start_audio();

float get_amplitude();

std::vector<float> get_fft_data();

struct GooBlob {
    glm::vec2 pos;
    glm::vec2 velocity;
    float radius;
};

class AudioPlayer {
public:
  void initGoo();
  void updateGoo(float dt, float bass);
  void init();
  void render(float *amp, float *time, float dt, int SCR_WIDTH, int SCR_HEIGHT);
  void loadSelectedTexture();
  int shadermode;
  int selectedImage;
  std::vector<std::string> textureNames;
  std::vector<const char *> textureItems;
private:
  std::vector<std::pair<int,int>> barRanges;
  std::string Shaderspath, imagepath;
  Shader circleShader, barShader, extraShader, spiralShader, globShader;
  GLuint vao, vbo, imagetex, ubo_fft;
  inline static constexpr int NUM_BARS = 200;
  const float SMOOTH_FACTOR = 0.1f;
  std::vector<GooBlob> gooBlobs;
};
#endif 
