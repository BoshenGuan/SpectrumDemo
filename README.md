# Spectrum controls for WPF
## Introduction
This repository is a WPF porting of spectrum plot controls (SpectrumAnalyzer and Waterfall) from SDRSharp <https://github.com/cgommel/sdrsharp>. The porting tries to make as few changes as possible to keep most of its original codes unchanged. The performance is fairly good as compared to the original application. <br/>

SDRSharp is a WinForm based application. To use its controls directly in a WPF application, `WindowsFormsHost` is usually adopted to act as a bridge between WinForm and WPF. But it is a hybrid solution and is not that straightforward. Alternatively, this WPF porting implements native `UserControl` in WPF. <br/>

As for drawing system, `System.Drawing` namespace (GDI+ actually) keeps unchanged, which is originally used in SDRSharp. <br/>

## Usage
All source files are under `Spectrum` folder. Copy it to project folder and add source files to the project tree in Visual Studio. The porting is implemented on Visual Studio 2013 and is retargeted to .Net Framework 4.5.1. <br/>

## Limitation
This porting currently does not support properties setting in XAML. This is because the `RenderSize` is evaluated only after first call to `OnRender` (before that the `RenderSize` is 0). The properties however are set in `InitializeComponent`, which is called before `OnRender`. <br/>

Setting properties in Loaded event does work as a work-around. See example project for more details.