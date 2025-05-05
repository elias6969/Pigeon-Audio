#include "audio.h"
#include <portaudio.h>
#include <fftw3.h>
#include <vector>
#include <mutex>

constexpr int FFT_SIZE = 1024;

static std::vector<float> fft_magnitudes(FFT_SIZE / 2);
static std::mutex fft_mutex;

// FFT state
static fftwf_plan fft_plan;
static float      input_buffer[FFT_SIZE];
static fftwf_complex output_buffer[FFT_SIZE];
static int buffer_index = 0;

static int audio_callback(const void* inputBuffer, void*, unsigned long frames,
                          const PaStreamCallbackTimeInfo*, PaStreamCallbackFlags, void*) {
    const float* in = static_cast<const float*>(inputBuffer);

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
    for (float v : data) sum += v;
    return sum / data.size();  // average energy
}
void start_audio() {
    fft_plan = fftwf_plan_dft_r2c_1d(FFT_SIZE, input_buffer, output_buffer, FFTW_MEASURE);

    Pa_Initialize();
    PaStream* stream;
    Pa_OpenDefaultStream(&stream, 1, 0, paFloat32, 44100, 256, audio_callback, nullptr);
    Pa_StartStream(stream);
}

std::vector<float> get_fft_data() {
    std::lock_guard<std::mutex> lock(fft_mutex);
    return fft_magnitudes;
}
