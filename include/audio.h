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

class AudioPlayer {
public:
  void init();
  void render(float *amp, float *time, float dt);
  int shadermode;
private:
  std::string Shaderspath, imagepath;
  Shader circleShader, barShader, extraShader;
  GLuint vao, vbo, imagetex, ubo_fft;
  inline static constexpr int NUM_BARS = 200;
  const float SMOOTH_FACTOR = 0.1f;
};
#endif 
