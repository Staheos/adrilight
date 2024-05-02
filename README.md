# Fork

A fork of [adrilight](https://github.com/fabsenet/adrilight) by fabsenet (originally [Bambilight](https://github.com/MrBoe/Bambilight) by MrBoe). Adrilight is an Ambilight clone for Windows: it reads the screen, works out the average color at the edges, and drives a WS2812b LED strip behind the monitor so the backlight matches what is on screen.

## What this fork adds

An audio-reactive brightness mode. On top of the normal screen-color behavior, the LED brightness can follow sound in real time.

The audio can come from two sources, switched with an Input/Output toggle in the Audio tab:

* **Input**: an audio input device, so a microphone or line-in.
* **Output**: what the computer is actually playing through the speakers, captured with WASAPI loopback.

When you flip the toggle the device list refills with the right set of devices and picks the system default for that direction. If no device is available nothing is selected and the LEDs just show the normal screen colors instead of breaking.

Under the hood: audio is captured through NAudio, run through a Hann window and an FFT (Accord.Math), grouped into bands, smoothed across frames, and used to scale the brightness of each spot. Brightness only gets modulated while capture is actually running, so a missing or unplugged device leaves the normal colors untouched instead of blacking out the strip.

The settings window UI was also cleaned up.

## Stack

C# / .NET Framework 4.7.2, WPF (MVVM). Screen capture through the Windows Desktop Duplication API (SharpDX). Audio through NAudio and NAudio.Wasapi. Arduino firmware in C++ with FastLED.

## Setup

For the full hardware build (LED strip, Arduino wiring, power supply) and the original setup guide, see the [upstream project](https://github.com/fabsenet/adrilight). The wiring and Arduino side are unchanged in this fork.

Software, in short:

1. Get an Arduino and a WS2812b strip, wire the strip around the back of the screen, and connect it to the Arduino.
2. Flash `adrilight.ino` (needs the FastLED library) to the Arduino.
3. Build this repo or run it, set the number of LEDs on each side, pick the COM port, and adjust the offset until the LEDs line up with the screen.
4. To use the audio mode, open the Audio tab, turn it on, and pick Input or Output plus a device.

## Data flow

PC (adrilight.exe) sends colors over USB to the Arduino (adrilight.ino), which drives the LEDs.

## License

Inherits the upstream project's license. See [fabsenet/adrilight](https://github.com/fabsenet/adrilight).
