#include "audio.h"
#include "filemanager.h"
#include "stb_image.h"
#include <GLFW/glfw3.h>
#include <cmath>
#include <expected>
#include <fftw3.h>
#include <glad/glad.h>
#include <iostream>
#include <iterator>
#include <mutex>
#include <ostream>
#include <portaudio.h>
#include <vector>

std::expected<GLuint, std::string> loadTexture(const char *path) {
  GLuint textureID;
  glGenTextures(1, &textureID);

  int width, height, nrChannels;
  stbi_set_flip_vertically_on_load(true);
  unsigned char *data = stbi_load(path, &width, &height, &nrChannels, 0);

  if (data) {
    GLenum format = (nrChannels == 4) ? GL_RGBA : GL_RGB;
    glBindTexture(GL_TEXTURE_2D, textureID);
    glTexImage2D(GL_TEXTURE_2D, 0, format, width, height, 0, format,
                 GL_UNSIGNED_BYTE, data);
    glGenerateMipmap(GL_TEXTURE_2D);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
  } else {
    return std::unexpected(std::string("Failed to load texture at path: ") +
                           path);
  }

  stbi_image_free(data);
  return textureID;
}

constexpr int FFT_SIZE = 1024;

static std::vector<float> fft_magnitudes(FFT_SIZE / 2);
static std::mutex fft_mutex;

// FFT state
static fftwf_plan fft_plan;
static float input_buffer[FFT_SIZE];
static fftwf_complex output_buffer[FFT_SIZE];
static int buffer_index = 0;

static int audio_callback(const void *inputBuffer, void *, unsigned long frames,
                          const PaStreamCallbackTimeInfo *,
                          PaStreamCallbackFlags, void *) {
  const float *in = static_cast<const float *>(inputBuffer);

  for (unsigned long i = 0; i < frames; ++i) {
    input_buffer[buffer_index++] = in[i];

    if (buffer_index >= FFT_SIZE) {
      buffer_index = 0;

      fftwf_execute(fft_plan);

      std::lock_guard<std::mutex> lock(fft_mutex);
      for (int i = 0; i < FFT_SIZE / 2; ++i) {
        float re = output_buffer[i][0];
        float im = output_buffer[i][1];
        fft_magnitudes[i] = sqrtf(re * re + im * im);
      }
    }
  }

  return paContinue;
}

float get_amplitude() {
  auto data = get_fft_data();
  float sum = 0.0f;
  for (float v : data)
    sum += v;
  return sum / data.size(); // average energy
}
void start_audio() {
  fft_plan = fftwf_plan_dft_r2c_1d(FFT_SIZE, input_buffer, output_buffer,
                                   FFTW_MEASURE);

  Pa_Initialize();
  PaStream *stream;
  Pa_OpenDefaultStream(&stream, 1, 0, paFloat32, 44100, 256, audio_callback,
                       nullptr);
  Pa_StartStream(stream);
}

std::vector<float> get_fft_data() {
  std::lock_guard<std::mutex> lock(fft_mutex);
  return fft_magnitudes;
}

float quadVertices[] = {-1.0f, -1.0f, 1.0f, -1.0f, -1.0f, 1.0f, 1.0f, 1.0f};

void AudioPlayer::init() {

  glGenVertexArrays(1, &vao);
  glGenBuffers(1, &vbo);

  glBindVertexArray(vao);
  glBindBuffer(GL_ARRAY_BUFFER, vbo);
  glBufferData(GL_ARRAY_BUFFER, sizeof(quadVertices), quadVertices,
               GL_STATIC_DRAW);
  glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 2 * sizeof(float), (void *)0);
  glEnableVertexAttribArray(0);

  GLsizei uboSize = sizeof(float) * 4 * NUM_BARS;
  glGenBuffers(1, &ubo_fft);
  glBindBuffer(GL_UNIFORM_BUFFER, ubo_fft);

  glBufferData(GL_UNIFORM_BUFFER, uboSize, nullptr, GL_DYNAMIC_DRAW);
  glBindBufferRange(GL_UNIFORM_BUFFER, 0, ubo_fft, 0, uboSize);
  start_audio();

  glEnable(GL_BLEND);
  glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
  VirtualFileSystem vfs("../assets/");
  std::string Shaderspath = vfs.getFullPath("Shaders/");
  std::string ImagePath = vfs.getFullPath("Textures/");
  auto result = loadTexture((ImagePath + "mm.jpg").c_str());
  if (!result) {
    std::cerr << "Texture error: " << result.error() << '\n';
  } else {
    imagetex = result.value();
  }
  circleShader.LoadShaders((Shaderspath + "circle.vs").c_str(),
                           (Shaderspath + "circle.fs").c_str());

  barShader.LoadShaders((Shaderspath + "circle.vs").c_str(),
                        (Shaderspath + "bars.fs").c_str());
}

void render_circle(float amplitude) {
  glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
  glClear(GL_COLOR_BUFFER_BIT);

  glBegin(GL_TRIANGLE_FAN);
  glColor3f(0.5f + amplitude, 0.2f, 0.7f);
  glVertex2f(0.0f, 0.0f);

  for (int i = 0; i <= 100; ++i) {
    float angle = i / 100.0f * 2.0f * M_PI;
    float x = std::cos(angle) * (0.3f + amplitude * 0.7f);
    float y = std::sin(angle) * (0.3f + amplitude * 0.7f);
    glVertex2f(x, y);
  }

  glEnd();
}

void AudioPlayer::render(float *amp, float *time) {
  std::cout << "Shader mode: " << shadermode << std::endl;

  if (shadermode == 0) { // bar visualizer
    barShader.use();
    barShader.setFloat("u_amplitude", *amp);
    barShader.setFloat("u_time", *time);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, imagetex);
    barShader.setInt("u_texture", 0);

    static float smoothedBinds[NUM_BARS] = {0.0f};
    auto raw = get_fft_data();
    for (int i = 0; i < NUM_BARS; ++i) {
      float v = (i < raw.size()) ? raw[i] : 0.0f;
      smoothedBinds[i] =
          SMOOTH_FACTOR * v + (1.0f - SMOOTH_FACTOR) * smoothedBinds[i];
    }

    static float padded[4 * NUM_BARS];
    for (int i = 0; i < NUM_BARS; ++i) {
      padded[i * 4 + 0] = smoothedBinds[i];
      padded[i * 4 + 1] = 0.0f;
      padded[i * 4 + 2] = 0.0f;
      padded[i * 4 + 3] = 0.0f;
    }

    glBindBuffer(GL_UNIFORM_BUFFER, ubo_fft);
    glBufferSubData(GL_UNIFORM_BUFFER, 0, sizeof(padded), padded);

    glBindVertexArray(vao);
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

  } else if (shadermode == 1) {
    barShader.use();
    barShader.setFloat("u_amplitude", *amp);
    barShader.setFloat("u_time", *time);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, imagetex);
    barShader.setInt("u_texture", 0);

    static float smoothedBinds[NUM_BARS] = {0.0f};
    auto raw = get_fft_data();
    float fftArray[NUM_BARS];
    for (int i = 0; i < NUM_BARS; ++i) {
      float v = (i < raw.size()) ? raw[i] : 0.0f;
      smoothedBinds[i] =
          SMOOTH_FACTOR * v + (1.0f - SMOOTH_FACTOR) * smoothedBinds[i];
    }

    // build a temporary padded array
    static float padded[4 * NUM_BARS];
    for (int i = 0; i < NUM_BARS; ++i) {
      padded[i * 4 + 0] = smoothedBinds[i]; // your value
      padded[i * 4 + 1] = 0.0f;             // pad
      padded[i * 4 + 2] = 0.0f;
      padded[i * 4 + 3] = 0.0f;
    }

    // upload all 3200 bytes
    glBindBuffer(GL_UNIFORM_BUFFER, ubo_fft);
    glBufferSubData(GL_UNIFORM_BUFFER, 0, sizeof(padded), padded);

    glBindVertexArray(vao);
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
  } else { // circle pulse
    circleShader.use();
    circleShader.setFloat("u_amplitude", *amp);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, imagetex);
    circleShader.setInt("u_texture", 0);

    glBindVertexArray(vao);
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
  }
}
