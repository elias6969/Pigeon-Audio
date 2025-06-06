#include "audio.h"
#include "filemanager.h"
#include "stb_image.h"
#include <GL/gl.h>
#include <GLFW/glfw3.h>
#include <algorithm>
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
#include "tinyfiledialogs.h"

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

void AudioPlayer::loadSelectedTexture() {
  VirtualFileSystem vfs;
  std::string ImagePath = vfs.getFullPath("Textures/");
  if (selectedImage >= 0 && selectedImage < textureNames.size()) {
    std::string fullImagePath = ImagePath + textureNames[selectedImage];
    auto result = loadTexture(fullImagePath.c_str());
    if (result) {
      imagetex = result.value();
      std::cout << "Image updated: " << fullImagePath << '\n';
    } else {
      std::cerr << "Failed to load texture: " << result.error() << '\n';
    }
  } else {
    std::cerr << "[loadSelectedTexture] Invalid image index: " << selectedImage
              << " (size: " << textureNames.size() << ")\n";
  }
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

void AudioPlayer::initGoo() {
  gooBlobs.clear();
  for (int i = 0; i < 32; ++i) {
    GooBlob blob;
    blob.pos = glm::vec2(drand48(), drand48()); // screen space: 0 to 1
    float angle = drand48() * 2.0 * M_PI;
    float speed = 0.02f + drand48() * 0.05f;
    blob.velocity = glm::vec2(cos(angle), sin(angle)) * speed;
    blob.radius = 0.015f;
    gooBlobs.push_back(blob);
  }
}

void AudioPlayer::updateGoo(float dt, float bass) {

  static float dropletCooldown = 0.0f;
  dropletCooldown -= dt;

  if (bass > 0.2f && dropletCooldown <= 0.0f && gooBlobs.size() < 64) {
    GooBlob droplet;
    droplet.pos = gooBlobs[0].pos;

    float angle = float(rand()) / RAND_MAX * 6.2831f;
    float speed = 0.3f + bass * 1.5f;
    droplet.velocity = glm::vec2(cos(angle), sin(angle)) * speed * 0.01f;
    droplet.radius = 0.015f;

    gooBlobs.push_back(droplet);
    dropletCooldown = 0.1f; // 100ms between droplets

    std::cout << "Spawned droplet! Total: " << gooBlobs.size() << "\n";
  }
}

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
  initGoo();

  glEnable(GL_BLEND);
  glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

  try {
    VirtualFileSystem vfs;
    std::string Shaderspath = vfs.getFullPath("Shaders/");
    std::string ImagePath = vfs.getFullPath("Textures/");

    for (const auto &entry :
         std::filesystem::directory_iterator("assets/Textures")) {
      if (entry.is_regular_file()) {
        textureNames.push_back(entry.path().filename().string());
      }
    }

    for (const auto &name : textureNames) {
      textureItems.push_back(name.c_str());
    }

    std::expected<GLuint, std::string> result;

    if (!textureNames.empty() && selectedImage >= 0 &&
        selectedImage < textureNames.size()) {

      result = loadTexture((ImagePath + textureNames[selectedImage]).c_str());

      if (result) {
        imagetex = result.value();
        std::cout << "Image loaded: " << textureNames[selectedImage] << "\n";
      } else {
        std::cerr << "Texture load failed: " << result.error() << "\n";
      }

    } else {
      std::cerr << "No textures available or selectedImage out of range!\n";
    }

    circleShader.LoadShaders((Shaderspath + "simple.vs").c_str(),
                             (Shaderspath + "simple.fs").c_str());
    barShader.LoadShaders((Shaderspath + "circle.vs").c_str(),
                          (Shaderspath + "bars.fs").c_str());
    spiralShader.LoadShaders((Shaderspath + "spiral.vs").c_str(),
                             (Shaderspath + "spiral.fs").c_str());
    globShader.LoadShaders((Shaderspath + "driplets.vs").c_str(),
                           (Shaderspath + "driplets.fs").c_str());

  } catch (std::exception &e) {
    std::cerr << "Fatal: " << e.what() << '\n';
  }

  shadermode = 0;
  barRanges.reserve(NUM_BARS);
  for (int i = 0; i < NUM_BARS; ++i) {
    float start = std::pow(float(i) / NUM_BARS, 2.2f) * (FFT_SIZE / 2);
    float end = std::pow(float(i + 1) / NUM_BARS, 2.2f) * (FFT_SIZE / 2);
    int b0 = std::clamp(int(start), 0, FFT_SIZE / 2 - 1);
    int b1 = std::clamp(int(end), 0, FFT_SIZE / 2 - 1);
    barRanges.emplace_back(b0, b1);
  }
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

void AudioPlayer::render(float *amp, float *time, float dt, int SCR_WIDTH,
                         int SCR_HEIGHT) {

  if (shadermode == 0) { // circle visalizuer or something
    //
    circleShader.use();
    float aspect = (float)SCR_WIDTH / (float)SCR_HEIGHT;
    glm::mat4 projection = glm::ortho(-aspect, aspect, -1.0f, 1.0f);
    circleShader.setMat4("u_projection", projection);
    circleShader.setFloat("u_time", *time);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, imagetex);
    circleShader.setInt("u_texture", 0);

    static float smoothedBinds[NUM_BARS] = {0.0f};
    static float runningAvg[NUM_BARS] = {0.0f};

    const float tau = 0.05f;
    float alpha = std::exp(-dt / tau);
    const float avgAlpha = 0.995f;  // keeps a long-term average
    const float compExp = 0.3f;     // <1 = stronger compression of spikes
    const float globalGain = 0.05f; // 0 = silent, 1 = full sensitivity
    const float smoothFact = 0.9f;  // closer to 1 = more temporal smoothing

    auto raw = get_fft_data();
    // Loops over each visual bar that will be drawn
    for (int i = 0; i < NUM_BARS; ++i) {
      int b0 = barRanges[i].first;
      int b1 = barRanges[i].second;
      float sum = 0.0f;
      for (int b = b0; b <= b1; b++)
        sum += raw[b];
      float v = (b1 >= b0) ? (sum / (b1 - b0 + 1)) : raw[b0];
      runningAvg[i] = avgAlpha * runningAvg[i] + (1.0f - avgAlpha) * v;
      float v_eq = v / (runningAvg[i] + 1e-6f);
      v_eq = std::pow(v_eq, compExp);
      v_eq *= globalGain;
      smoothedBinds[i] = alpha * smoothedBinds[i] + (1.0f - alpha) * v_eq;
    }

    static float padded[4 * NUM_BARS];
    for (int i = 0; i < NUM_BARS; ++i) {
      padded[i * 4 + 0] = smoothedBinds[i];
      padded[i * 4 + 1] = 0.0f;
      padded[i * 4 + 2] = 0.0f;
      padded[i * 4 + 3] = 0.0f;
    }

    glBindBuffer(GL_UNIFORM_BUFFER, ubo_fft);
    glBufferData(GL_UNIFORM_BUFFER, sizeof(padded), nullptr, GL_DYNAMIC_DRAW);
    glBufferSubData(GL_UNIFORM_BUFFER, 0, sizeof(padded), padded);

    glBindVertexArray(vao);
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

  } else if (shadermode == 1) { // Bar Visualiser
    barShader.use();
    float aspect = (float)SCR_WIDTH / (float)SCR_HEIGHT;
    glm::mat4 projection = glm::ortho(-aspect, aspect, -1.0f, 1.0f);
    barShader.setMat4("u_projection", projection);
    barShader.setFloat("u_amplitude", *amp);
    barShader.setFloat("u_time", *time);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, imagetex);
    barShader.setInt("u_texture", 0);

    static float smoothedBinds[NUM_BARS] = {0.0f};
    static float runningAvg[NUM_BARS] = {0.0f};

    const float tau = 0.05f;
    float alpha = std::exp(-dt / tau);
    const float avgAlpha = 0.995f;  // keeps a long-term average
    const float compExp = 0.3f;     // <1 = stronger compression of spikes
    const float globalGain = 0.05f; // 0 = silent, 1 = full sensitivity
    const float smoothFact = 0.9f;  // closer to 1 = more temporal smoothing

    auto raw = get_fft_data();
    // Loops over each visual bar that will be drawn
    for (int i = 0; i < NUM_BARS; ++i) {
      int b0 = barRanges[i].first;
      int b1 = barRanges[i].second;
      float sum = 0.0f;
      for (int b = b0; b <= b1; b++)
        sum += raw[b];
      float v = (b1 >= b0) ? (sum / (b1 - b0 + 1)) : raw[b0];
      runningAvg[i] = avgAlpha * runningAvg[i] + (1.0f - avgAlpha) * v;
      float v_eq = v / (runningAvg[i] + 1e-6f);
      v_eq = std::pow(v_eq, compExp);
      v_eq *= globalGain;
      smoothedBinds[i] = alpha * smoothedBinds[i] + (1.0f - alpha) * v_eq;
    }

    static float padded[4 * NUM_BARS];
    for (int i = 0; i < NUM_BARS; ++i) {
      padded[i * 4 + 0] = smoothedBinds[i];
      padded[i * 4 + 1] = 0.0f;
      padded[i * 4 + 2] = 0.0f;
      padded[i * 4 + 3] = 0.0f;
    }

    glBindBuffer(GL_UNIFORM_BUFFER, ubo_fft);
    glBufferData(GL_UNIFORM_BUFFER, sizeof(padded), nullptr, GL_DYNAMIC_DRAW);
    glBufferSubData(GL_UNIFORM_BUFFER, 0, sizeof(padded), padded);

    glBindVertexArray(vao);
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
  } else if (shadermode == 2) {

    auto fft = get_fft_data();
    float bass = 0.0f;
    float mid = 0.0f;
    float treble = 0.0f;

    int bass_end = FFT_SIZE / 32;
    int mid_start = FFT_SIZE / 32;
    int mid_end = FFT_SIZE / 8;
    int treble_start = FFT_SIZE / 8;

    for (int i = 0; i < fft.size(); ++i) {
      if (i < bass_end)
        bass += fft[i];
      else if (i < mid_end)
        mid += fft[i];
      else
        treble += fft[i];
    }

    bass /= bass_end;
    mid /= (mid_end - mid_start);
    treble /= (fft.size() - treble_start);
    updateGoo(dt, bass);
    globShader.use();
    // rainShader.setFloat("u_amplitude", *amp);
    globShader.setFloat("u_time", *time);
    globShader.setFloat("u_bass", bass);
    globShader.setFloat("u_mid", mid);
    globShader.setFloat("u_treble", treble);
    globShader.setInt("u_sceneTex", 0);

    for (auto &blob : gooBlobs) {
      blob.pos += blob.velocity * dt;

      // Wrap-around logic (toroidal space)
      if (blob.pos.x < 0.0f)
        blob.pos.x += 1.0f;
      if (blob.pos.x > 1.0f)
        blob.pos.x -= 1.0f;
      if (blob.pos.y < 0.0f)
        blob.pos.y += 1.0f;
      if (blob.pos.y > 1.0f)
        blob.pos.y -= 1.0f;

      // Optional: wobble based on bass
      blob.pos += glm::vec2(0.002f * sin((*time + blob.pos.y) * 10.0f),
                            0.002f * cos((*time + blob.pos.x) * 10.0f)) *
                  bass;
    }

    for (int i = 0; i < gooBlobs.size(); ++i) {
      std::string name = "u_blobs[" + std::to_string(i) + "]";
      globShader.setVec2(name.c_str(), gooBlobs[i].pos);
      // std::cout << "Blob " << i << ": " << gooBlobs[i].pos.x << ", "
      //          << gooBlobs[i].pos.y << '\n';
    }
    globShader.setInt("u_blobCount", gooBlobs.size());
    glBindVertexArray(vao);
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
  }
}
