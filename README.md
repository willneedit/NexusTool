# Nexus Tool

Lightweight tool for managing the iCUE Nexus companion touchscreen display

## Introduction

The (Corsair) iCUE Nexus is a 640x48 pixels touchscreen display, designed to be attached to a keyboard, used to show a status screen or for other uses, providing buttons like for launching custom apps, and more.

Sadly, the accompanied software is mainly geared towards RGB lighting, bloated ... and Windows only.

Leaving Windows 11 - the recent update shenanigans were the straw that broke the camel's back - I decided to create a **small** tool to make it work.

## Features

- Simple and intended to be cross-platform
- Plays the firmware embedded animations (just for sake of completeness...)
- Sets brightness (0-100, with 0 being completely off)
- Loads and shows an image (preferably 640x48 RGBA32 image)
- Waits for touch for x secs and reports touch or swipe, or no action

## Compiling & Installation

- **Requires dotnet8**
- `dotnet build`

To make the device available for non-root users, you have to import the udev rule, too.

1. `sudo cp _etc_udev_rules.d/99-icuenexus.rules /etc/udev/rules.d`
2. `sudo udevadm control -R && sudo udevadm trigger`
3. Log out and back in, or reset your machine, if necessary

## Usage

Self explanatory. `NexusTool -h` gives you the help page.

### Touch responses

For example, `NexusTool -t 5` waits for up to five seconds for a touch, and emits a one line of response.

- `--` Timeout - no touch.
- `++ (value)` Steady touch, between 0-639 with 0 being left
- `+- (value)` Jittery touch, slipped finger.
- `<-` Swipe left
- `->` Swipe right
