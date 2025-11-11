#pragma once

#include "Camera.h"
#include "Shader.h"
#include <cmath>
#include <fftw3.h>
#include <glm/fwd.hpp>
#include <iostream>
#include <mutex>
#include <portaudio.h>
#include <vector>

class Shader;
class Camera;

using GLuint = unsigned int;

void start_audio();
float get_amplitude();
std::vector<float> get_fft_data();

struct PVertex {
  float x, y, size, alpha;
};
// ----- Visual elements -----
struct GooBlob {
  glm::vec2 pos{};
  glm::vec2 velocity{};
  float radius{0.0f};
};

struct Particle {
  glm::vec2 pos{};
  glm::vec2 velocity{};
  float life{0.0f}; // seconds remaining
  float size{0.0f}; // visual size (screen-ish units)
  float hue{0.0f};  // optional, for coloring
};

struct VisualizerSetting {
  glm::vec3 barColor = glm::vec3(0.2f, 1.0f, 0.5f);
  bool animateHue = false;
};

// ----- Main controller -----
class AudioPlayer {
public:
  // ---- Lifecycle ----
  void init(); // create GL objects, load shaders/textures, audio, goo, etc.
  void render(float *amp, float *time, float dt, int SCR_WIDTH, int SCR_HEIGHT,
              float globalGain);

  // ---- Content / assets ----
  void loadSelectedTexture();

  // ---- Goo blobs ----
  void initGoo();
  void updateGoo(float dt, float bass);

  // ---- Beat FX / particles ----
  void triggerBeatEvent(int band, float strength);
  void updateParticles(float dt);

  // ---- Mode / UI state ----
  int shadermode{0}; // 0=simple, 1=circle, 2=goo (as per your .cpp)
  int simpleType{0};
  int selectedImage{-1};
  std::vector<std::string> textureNames;  // filenames
  std::vector<const char *> textureItems; // raw C-strings for UI lists

  // ---- Beat detection state ----
  float beatFluxAvg{0.0f};
  float beatFluxVar{0.0f};
  float beatThresholdK{1.5f}; // higher = fewer beats
  float flash{0.0f};          // decays; use in shaders for glow/flash
  float shake{0.0f};          // decays; small camera jitter
  int paletteIndex{0};        // cycles on beat
  int paletteCount{4};        // how many palettes in shader
  float fluxAvgBass = 0, fluxVarBass = 0;
  float fluxAvgMid = 0, fluxVarMid = 0;
  float fluxAvgTre = 0, fluxVarTre = 0;
  float beatK = 1.4f; // per-band threshold factor (tweak in UI if you like)
  std::vector<PVertex> pv;
  glm::vec3 debugTint = glm::vec3(0.0f);
  std::vector<float> prevMag; // previous spectrum for spectral flux

  // ---- Particles ----
  std::vector<Particle> particles;
  int burstCount{160};
  float burstSpeed{0.35f}; // screen units / sec
  float burstLife{0.8f};   // seconds

  // ---- Sensitivity (overall input gain) ----
  float sensitivity{
      0.12f};

  // ---- Settings parameters ----
  int imagefitmode;
  VisualizerSetting settings;

private:
  // ---- FFT-to-bar mapping ----
  std::vector<std::pair<int, int>> barRanges; // [b0,b1] bins per visual bar

  // ---- Paths ----
  std::string shadersPath;
  std::string imagePath;

  // ---- Shaders ----
  Shader simpleShader;
  Shader simpleShaderWave;
  Shader simpleShaderGlow;
  Shader simpleShaderSchizo1;
  Shader simpleShaderSchizo2;
  Shader simpleShaderSchizo3;
  Shader simpleShaderSchizo4;
  Shader simpleShaderSchizo5;

  Shader circleShader;
  Shader extraShader;
  Shader spiralShader;
  Shader globShader;
  Shader particleShader;

  // ---- GL objects ----
  GLuint vao{0}, vbo{0};
  GLuint imagetex{0};
  GLuint ubo_fft{0};
  GLuint particleVAO = 0, particleVBO = 0, particleProg = 0;
  // ---- Visual params ----
  inline static constexpr int NUM_BARS = 200;
  static constexpr float SMOOTH_FACTOR = 0.1f;

  // ---- Goo state ----
  std::vector<GooBlob> gooBlobs;
};
