[![Build Status](https://img.shields.io/github/actions/workflow/status/yourusername/PigeonAudio/ci.yml?branch=main)](https://github.com/yourusername/PigeonAudio/actions) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

# PigeonAudio

Real-time audio visualization with modern OpenGL, FFT analysis, and an interactive Dear ImGui GUI.

---

## 📋 Table of Contents

1. [Features](#-features)  
2. [Demo](#-demo)  
3. [Dependencies](#-dependencies)  
4. [Getting Started](#-getting-started)  
   - [Clone & Build (CMake)](#clone--build-cmake)  
   - [Manual Build (g++)](#manual-build-g)  
5. [Assets Layout](#-assets-layout)  
6. [Usage & Controls](#-usage--controls)  
7. [Project Structure](#-project-structure)  
8. [Contributing](#-contributing)  
9. [License](#-license)  

---

## 🚀 Features

- **Live audio capture & FFT**  
  Capture microphone input via PortAudio and compute frequency spectra with FFTW3.  
- **Shader-powered visualizations**  
  - **Circle**: radial waveform shader  
  - **Bars**: spectrum bar graph shader  
- **Immediate-mode GUI**  
  Toggle modes, adjust parameters, and debug via Dear ImGui overlay.  
- **Robust asset management**  
  Locate shaders/textures at runtime with our `VirtualFileSystem`.  
- **OpenGL error reporting**  
  Real-time debug callbacks following LearnOpenGL best practices.  

---

## 🎬 Demo

![Circular Mode](docs/screenshots/circle_mode.png)  
![Bars Mode](docs/screenshots/bars_mode.png)  

---

## 🔧 Dependencies

- **C++17** or newer  
- **OpenGL 4.2 Core**  
- [GLFW](https://www.glfw.org/) (window & context)  
- [GLAD](https://glad.dav1d.de/) (loader)  
- [GLM](https://github.com/g-truc/glm) (math)  
- [Dear ImGui](https://github.com/ocornut/imgui) (GUI)  
- [PortAudio](http://www.portaudio.com/) (audio I/O)  
- [FFTW3](http://www.fftw.org/) (FFT)  
- [stb_image](https://github.com/nothings/stb) (texture)  

> _All dependencies should be installed with their development headers before building._  

---

## 🛠️ Getting Started

### Clone & Build (CMake)

```bash
git clone https://github.com/yourusername/PigeonAudio.git
cd PigeonAudio
mkdir build && cd build
cmake ..
make
# Executable: build/PigeonAudio
Manual Build (g++)
bash
Copy
Edit
g++ -std=c++17 \
    main.cpp filemanager.cpp OpenGLerrorreporting.cpp audio.cpp \
    -I/path/to/glad/include \
    -I/path/to/glfw/include \
    -I/path/to/imgui \
    -lglfw -lGL -ldl -lglad -lportaudio -lfftw3 -pthread \
    -o PigeonAudio
📁 Assets Layout
Place your shaders and textures under assets/ at project root:

arduino
Copy
Edit
assets/
├── Shaders/
│   ├── circle.vs
│   ├── circle.fs
│   ├── bars.vs
│   └── bars.fs
└── Textures/
    └── mm.jpg
🎮 Usage & Controls
bash
Copy
Edit
./PigeonAudio
F: Show ImGui panel

G: Hide ImGui panel

Combo Box: Select “Circle” or “Bars” shader

ESC: Exit application

🗂️ Project Structure
bash
Copy
Edit
├── CMakeLists.txt           # Build configuration
├── assets/                  # Shaders & textures
├── docs/
│   └── screenshots/         # Demo images
├── main.cpp                 # Init, render loop & ImGui setup
├── filemanager.{h,cpp}      # VirtualFileSystem for assets
├── OpenGLerrorreporting.{h,cpp} # Debug callback utilities
├── audio.{h,cpp}            # PortAudio capture & FFT logic
├── Shader.h                 # GLSL loader / utility class
├── Camera.h                 # Optional 3D camera class
└── README.md                # ← you are here
🤝 Contributing
Fork the repo

Create a feature branch (git checkout -b feature/foo)

Commit your changes (git commit -m "Add foo")

Push to the branch (git push origin feature/foo)

Open a Pull Request

Please follow the existing code style and include relevant tests/examples.
