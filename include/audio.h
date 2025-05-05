#ifndef AUDIO_H
#define AUDIO_H
#include <portaudio.h>
#include <fftw3.h>
#include <vector>
#include <mutex>
#include <cmath>
#include <iostream>

void start_audio();

float get_amplitude();

std::vector<float> get_fft_data();

#endif 
