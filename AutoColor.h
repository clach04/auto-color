
/* cakelisp_cache/STB/AutoColor.cake.hpp */

#pragma once
#include "stdbool.h"
extern const char* gAutoColorCopyrightString;
extern bool gAutoColorShouldPrint;
bool autoColorGetCurrentBackgroundFilename(char* wallpaperOut, unsigned int wallpaperOutSize,
                                           const char** errorString);
typedef unsigned char AutoColor[3];
typedef struct AutoColorStruct
{
	unsigned char x;
	unsigned char y;
	unsigned char z;
} AutoColorStruct;
typedef struct AutoColorFloat
{
	float x;
	float y;
	float z;
} AutoColorFloat;
bool autoColorPickFromCurrentBackground(AutoColor* base16ColorsOut);

/* cakelisp_cache/STB/Image.cake.hpp */

#pragma once
#include "stb_image.h"

/* cakelisp_cache/STB/Image.cake.cpp */

/* #include "Image.cake.hpp" */

#define STB_IMAGE_IMPLEMENTATION

#define STBI_FAILURE_USERMSG

#include "stb_image.h"

/* cakelisp_cache/STB/AutoColor.cake.cpp */

#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

const char* gAutoColorCopyrightString =
    "Auto Color\n"
    "Created by Macoy Madson <macoy@macoy.me>.\n"
    "https://macoy.me/code/macoy/auto-color\n"
    "Copyright (c) 2021 Macoy Madson.\n"
    "\n"
    "Auto Color is free software: you can redistribute it and/or modify\n"
    "it under the terms of the GNU General Public License as published by\n"
    "the Free Software Foundation, either version 3 of the License, or\n"
    "(at your option) any later version.\n"
    "\n"
    "Auto Color is distributed in the hope that it will be useful,\n"
    "but WITHOUT ANY WARRANTY; without even the implied warranty of\n"
    "MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the\n"
    "GNU General Public License for more details.\n"
    "\n"
    "You should have received a copy of the GNU General Public License\n"
    "along with Auto Color.  If not, see <https://www.gnu.org/licenses/>.\n"
    "\n"
    "--------------------------------------------------------------------------------\n"
    "\n"
    "Uses modified color conversion functions with the following preamble:\n"
    "Ported by Renaud Bédard (@renaudbedard) from original code from Tanner Helland:\n"
    "http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/\n"
    "Color space functions translated from HLSL versions on Chilli Ant (by Ian Taylor):\n"
    "http://www.chilliant.com/rgb2hsv.html\n"
    "Licensed and released under Creative Commons 3.0 Attribution:\n"
    "https://creativecommons.org/licenses/by/3.0/\n"
    "\n"
    "Copied from https://github.com/mixaal/imageprocessor.\n"
    "Modified by Macoy Madson.\n"
    "\n"
    "--------------------------------------------------------------------------------\n"
    "\n"
    "Uses modified code from uriparser:\n"
    "uriparser - RFC 3986 URI parsing library\n"
    "\n"
    "Copyright (C) 2007, Weijia Song <songweijia@gmail.com>\n"
    "Copyright (C) 2007, Sebastian Pipping <sebastian@pipping.org>\n"
    "All rights reserved.\n"
    "\n"
    "Redistribution and use in source  and binary forms, with or without\n"
    "modification, are permitted provided  that the following conditions\n"
    "are met:\n"
    "\n"
    "    1. Redistributions  of  source  code   must  retain  the  above\n"
    "       copyright notice, this list  of conditions and the following\n"
    "       disclaimer.\n"
    "\n"
    "    2. Redistributions  in binary  form  must  reproduce the  above\n"
    "       copyright notice, this list  of conditions and the following\n"
    "       disclaimer  in  the  documentation  and/or  other  materials\n"
    "       provided with the distribution.\n"
    "\n"
    "    3. Neither the  name of the  copyright holder nor the  names of\n"
    "       its contributors may be used  to endorse or promote products\n"
    "       derived from  this software  without specific  prior written\n"
    "       permission.\n"
    "\n"
    "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS\n"
    "\"AS IS\" AND  ANY EXPRESS OR IMPLIED WARRANTIES,  INCLUDING, BUT NOT\n"
    "LIMITED TO,  THE IMPLIED WARRANTIES OF  MERCHANTABILITY AND FITNESS\n"
    "FOR  A  PARTICULAR  PURPOSE  ARE  DISCLAIMED.  IN  NO  EVENT  SHALL\n"
    "THE  COPYRIGHT HOLDER  OR CONTRIBUTORS  BE LIABLE  FOR ANY  DIRECT,\n"
    "INDIRECT, INCIDENTAL, SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES\n"
    "(INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT OF SUBSTITUTE GOODS OR\n"
    "SERVICES; LOSS OF USE, DATA,  OR PROFITS; OR BUSINESS INTERRUPTION)\n"
    "HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,\n"
    "STRICT  LIABILITY,  OR  TORT (INCLUDING  NEGLIGENCE  OR  OTHERWISE)\n"
    "ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED\n"
    "OF THE POSSIBILITY OF SUCH DAMAGE.";

bool gAutoColorShouldPrint = false;

#include "gio/gio.h"

const char* uriUnescapeInPlaceEx(char* inOut);

bool autoColorGetCurrentBackgroundFilename(char* wallpaperOut, unsigned int wallpaperOutSize,
                                           const char** errorString)
{
	GSettings* gSettings = g_settings_new("org.gnome.desktop.background");
	if (!(gSettings))
	{
		(*errorString) = "Unable to get GTK settings for org.gnome.desktop.background";
		return false;
	}
	gchar* background = g_settings_get_string(gSettings, "picture-uri");
	if (!(background))
	{
		(*errorString) = "Unable to get picture-uri from org.gnome.desktop.background";
		g_object_unref(gSettings);
		gSettings = NULL;
		return false;
	}
	g_object_unref(gSettings);
	gSettings = NULL;
	const char* fileUri = "file://";
	size_t fileUriPrefixLength = strlen(fileUri);
	if (!((0 == strncmp(fileUri, background, fileUriPrefixLength))))
	{
		(*errorString) = "Unable to process picture-uri: uri type not supported";
		g_free(background);
		return false;
	}
	snprintf(wallpaperOut, wallpaperOutSize, "%s", (background + fileUriPrefixLength));
	g_free(background);
	bool foundBadChar = false;
	for (char* currentChar = wallpaperOut; (*currentChar); ++currentChar)
	{
		if (('%' == (*currentChar)))
		{
			foundBadChar = true;
			break;
		}
	}
	if (foundBadChar)
	{
		uriUnescapeInPlaceEx(wallpaperOut);
	}
	return true;
}

typedef struct AutoColorImage
{
	int width;
	int height;
	unsigned char* pixelData;
} AutoColorImage;

static void autoColorImageDestroy(AutoColorImage* imageData)
{
	stbi_image_free(imageData->pixelData);
}

static bool autoColorLoadImage(const char* imageToLoad, AutoColorImage* imageDataOut)
{
	int numPixelComponents = 0;
	int numDesiredChannels = 3;
	unsigned char* pixelData =
	    stbi_load(imageToLoad, (&imageDataOut->width), (&imageDataOut->height),
	              (&numPixelComponents), numDesiredChannels);
	if (!(pixelData))
	{
		if (gAutoColorShouldPrint)
		{
			fprintf(stderr, "error: failed to load %s with message: %s\n", imageToLoad,
			        stbi_failure_reason());
		}
		return false;
	}
	imageDataOut->pixelData = pixelData;
	return true;
}

static unsigned char autoColorGetLightness(AutoColor color)
{
	int maxComponent = 0;
	int minComponent = 255;
	for (int i = 0; (i < 3); ++i)
	{
		if ((color[i] > maxComponent))
		{
			maxComponent = color[i];
		}
		if ((color[i] < minComponent))
		{
			minComponent = color[i];
		}
	}
	return ((maxComponent + minComponent) / 2);
}

static float autoColorClampZeroToOne(float value)
{
	if ((value < 0.f))
	{
		return 0.f;
	}
	if ((value > 1.0f))
	{
		return 1.0f;
	}
	return value;
}

static AutoColorStruct autoColorFloatToChar(AutoColorFloat color)
{
	AutoColorStruct converted = {((unsigned char)round((autoColorClampZeroToOne(color.x) * 255))),
	                             ((unsigned char)round((autoColorClampZeroToOne(color.y) * 255))),
	                             ((unsigned char)round((autoColorClampZeroToOne(color.z) * 255)))};
	return converted;
}

static AutoColorFloat autoColorCharToFloat(AutoColor color)
{
	AutoColorFloat converted = {(color[0] / 255.f), (color[1] / 255.f), (color[2] / 255.f)};
	return converted;
}

static AutoColorFloat autoColorHueToRgb(float hue)
{
	float r = autoColorClampZeroToOne((fabs(((hue * 6.0f) - 3.0f)) - 1.0f));
	float g = autoColorClampZeroToOne((2.0f - fabs(((hue * 6.0f) - 2.0f))));
	float b = autoColorClampZeroToOne((2.0f - fabs(((hue * 6.0f) - 4.0f))));
	AutoColorFloat rgb = {r, g, b};
	return rgb;
}

static AutoColorFloat autoColorHslToRgb(AutoColorFloat hsl)
{
	AutoColorFloat rgb = autoColorHueToRgb(hsl.x);
	float c = ((1.0f - fabs(((2.0f * hsl.z) - 1.0f))) * hsl.y);
	rgb.x = (((rgb.x - 0.5f) * c) + hsl.z);
	rgb.y = (((rgb.y - 0.5f) * c) + hsl.z);
	rgb.z = (((rgb.z - 0.5f) * c) + hsl.z);
	return rgb;
}

static float colorConversionEpsilon = 1e-10;

static AutoColorFloat autoColorRgbToHcv(AutoColorFloat rgb)
{
	typedef struct ColorVec4
	{
		float x;
		float y;
		float z;
		float w;
	} ColorVec4;
	ColorVec4 p;
	ColorVec4 q;
	if ((rgb.y < rgb.z))
	{
		p.x = rgb.z;
		p.y = rgb.y;
		p.z = -1.0f;
		p.w = 2.0f / 3.0f;
	}
	else
	{
		p.x = rgb.y;
		p.y = rgb.z;
		p.z = 0.0f;
		p.w = -1.0f / 3.0f;
	}
	if ((rgb.x < p.x))
	{
		q.x = p.x;
		q.y = p.y;
		q.z = p.w;
		q.w = rgb.x;
	}
	else
	{
		q.x = rgb.x;
		q.y = p.y;
		q.z = p.z;
		q.w = p.x;
	}
	float c = (q.x - ((q.w < q.y) ? q.w : q.y));
	float h = fabs((((q.w - q.y) / ((6.0f * c) + colorConversionEpsilon)) + q.z));
	AutoColorFloat packedColor = {h, c, q.x};
	return packedColor;
}

static AutoColorFloat autoColorRgbToHsl(AutoColorFloat rgb)
{
	AutoColorFloat HCV = autoColorRgbToHcv(rgb);
	float L = (HCV.z - (HCV.y * 0.5f));
	float S = (HCV.y / ((1.0f - fabs(((L * 2.0f) - 1.0f))) + colorConversionEpsilon));
	AutoColorFloat packedColor = {HCV.x, S, L};
	return packedColor;
}

static int testAutoColorConversions()
{
	AutoColorFloat testHsl = {0.25f, 0.8f, 0.2f};
	AutoColorStruct testCharHsl = autoColorFloatToChar(testHsl);
	AutoColorFloat colorRgb = autoColorHslToRgb(testHsl);
	AutoColorStruct colorCharRgb = autoColorFloatToChar(colorRgb);
	AutoColorStruct colorCharHsl = autoColorFloatToChar(autoColorRgbToHsl(colorRgb));
	if (gAutoColorShouldPrint)
	{
		fprintf(stderr,
		        "HSL:         %3d %3d %3d\nRGB:         %3d %3d %3d\nBack to HSL: %3d %3d %3d\n",
		        testCharHsl.x, testCharHsl.y, testCharHsl.z, colorCharRgb.x, colorCharRgb.y,
		        colorCharRgb.z, colorCharHsl.x, colorCharHsl.y, colorCharHsl.z);
	}
	if ((0 != memcmp((&colorCharHsl), (&testCharHsl), sizeof(AutoColorStruct))))
	{
		return 1;
	}
	return 0;
}

static unsigned char autoColorPickColorsByThreshold(AutoColorImage* imageData,
                                                    AutoColor* colorPaletteOut,
                                                    unsigned char numColorsRequested)
{
	if (!((imageData && colorPaletteOut && numColorsRequested)))
	{
		return 0;
	}
	AutoColor colorSamples[512];
	int sampleSkipX = 0;
	int sampleSkipY = 0;

	{
		int samplesPerX = 1;
		int samplesPerY = 1;
		float samplesPerAxis = sqrtf((sizeof(colorSamples) / sizeof(colorSamples[0])));
		samplesPerX = (samplesPerAxis * (imageData->width / ((float)imageData->height)));
		samplesPerY = (samplesPerAxis * (((float)imageData->height) / imageData->width));
		int numColorSamples = (samplesPerX * samplesPerY);
		sampleSkipX = (imageData->width / (samplesPerX - 1));
		sampleSkipY = (imageData->height / (samplesPerY - 1));
		if (gAutoColorShouldPrint)
		{
			fprintf(stderr,
			        "Samples: %dx%d for total of %d samples. Sample every %dx%d pixel of the %dx%d "
			        "image\n",
			        samplesPerX, samplesPerY, numColorSamples, sampleSkipX, sampleSkipY,
			        imageData->width, imageData->height);
		}
	}
	AutoColor* currentSampleWrite = colorSamples;
	for (int y = 0; (y < imageData->height); y = (y + sampleSkipY))
	{
		for (int x = 0; (x < imageData->width); x = (x + sampleSkipX))
		{
			int pixelIndex = (3 * ((y * imageData->width) + x));
			unsigned char* pixelColorComponent = (&imageData->pixelData[pixelIndex]);
			for (int i = 0; (i < 3); ++i)
			{
				(*currentSampleWrite)[i] = pixelColorComponent[i];
			}
			++currentSampleWrite;
		}
	}
	int numSamples = (currentSampleWrite - colorSamples);
	if (gAutoColorShouldPrint)
	{
		fprintf(stderr, "Sampled %d pixels\n", numSamples);
	}
	int numDistinctColors = 0;
	int distinctnessThreshold = 50;
	for (int sampleIndex = 0; (sampleIndex < numSamples); ++sampleIndex)
	{
		bool isDistinct = true;
		for (int distinctColorIndex = 0; (distinctColorIndex < numDistinctColors);
		     ++distinctColorIndex)
		{
			int colorDifference[3];
			for (int i = 0; (i < 3); ++i)
			{
				colorDifference[i] = abs((((int)colorSamples[sampleIndex][i]) -
				                          ((int)colorPaletteOut[distinctColorIndex][i])));
			}
			int totalDifference = 0;
			for (int i = 0; (i < 3); ++i)
			{
				totalDifference = (totalDifference + colorDifference[i]);
			}
			if (!((totalDifference >= distinctnessThreshold)))
			{
				isDistinct = false;
				break;
			}
		}
		if (!(isDistinct))
		{
			continue;
		}
		for (int i = 0; (i < 3); ++i)
		{
			colorPaletteOut[numDistinctColors][i] = colorSamples[sampleIndex][i];
		}
		++numDistinctColors;
		if ((numDistinctColors >= numColorsRequested))
		{
			break;
		}
	}
	return numDistinctColors;
}

static int autoColorSortHslColorFloatDarkestFirst(const void* a, const void* b)
{
	AutoColorFloat* aValue = ((AutoColorFloat*)a);
	AutoColorFloat* bValue = ((AutoColorFloat*)b);
	if ((aValue->z != bValue->z))
	{
		return ((aValue->z < bValue->z) ? -1 : 1);
	}
	if ((aValue->y != bValue->y))
	{
		return ((aValue->y < bValue->y) ? -1 : 1);
	}
	return ((aValue->x < bValue->x) ? -1 : 1);
}

static bool autoColorIsWithinContrastRange(AutoColorFloat color, float backgroundLightness,
                                           float minimumContrast, float maximumContrast)
{
	float contrast = (color.z - backgroundLightness);
	if ((contrast < minimumContrast))
	{
		return false;
	}
	if ((contrast > maximumContrast))
	{
		return false;
	}
	return true;
}

static AutoColorFloat autoColorClampWithinContrastRange(AutoColorFloat color,
                                                        float backgroundLightness,
                                                        float minimumContrast,
                                                        float maximumContrast)
{
	float contrast = (color.z - backgroundLightness);
	if ((contrast < minimumContrast))
	{
		color.z = (color.z + (minimumContrast - contrast));
	}
	if ((contrast > maximumContrast))
	{
		color.z = (color.z - (contrast - maximumContrast));
	}
	return color;
}

static void autoColorCreateBase16ThemeFromColors(AutoColor* colorPalette,
                                                 unsigned char numColorsInPalette,
                                                 AutoColorFloat* workSpace,
                                                 AutoColor* base16ColorsOut)
{
	if (!((colorPalette && numColorsInPalette && workSpace && base16ColorsOut)))
	{
		return;
	}
	float maximumBackgroundBrightnessThresholds[] = {0.08f, 0.15f, 0.2f, 0.25f, 0.3f, 0.4f, 0.45f};
	float minimumDeEmphasizedTextContrast = 0.3f;
	float minimumTextContrast = 0.43f;
	float maximumTextContrast = 0.65f;
	float darkestBrightenPerRepeat = 0.03f;
	float darkBrightenPerRepeat = 0.07f;
	float lightDarkenPerRepeat = 0.01f;
	typedef enum AutoColorSelectionMethod
	{
		pickDarkestColorForceDarkThreshold,
		pickDarkestHighContrastColor,
		pickHighContrastBrightColor,
	} AutoColorSelectionMethod;
	typedef struct AutoColorBase16Color
	{
		const char* description;
		AutoColorSelectionMethod method;
	} AutoColorBase16Color;
	AutoColorBase16Color selectionMethods[16] = {
	    {"base00 - Default Background", pickDarkestColorForceDarkThreshold},
	    {"base01 - Lighter Background (Used for status bars)", pickDarkestColorForceDarkThreshold},
	    {"base02 - Selection Background", pickDarkestColorForceDarkThreshold},
	    {"base03 - Comments, Invisibles, Line Highlighting", pickDarkestHighContrastColor},
	    {"base04 - Dark Foreground (Used for status bars)", pickDarkestHighContrastColor},
	    {"base05 - Default Foreground, Caret, Delimiters, Operators", pickDarkestHighContrastColor},
	    {"base06 - Light Foreground (Not often used)", pickDarkestColorForceDarkThreshold},
	    {"base07 - Light Background (Not often used)", pickDarkestColorForceDarkThreshold},
	    {"base08 - Variables, XML Tags, Markup Link Text, Markup Lists, Diff Deleted",
	     pickHighContrastBrightColor},
	    {"base09 - Integers, Boolean, Constants, XML Attributes, Markup Link Url",
	     pickHighContrastBrightColor},
	    {"base0A - Classes, Markup Bold, Search Text Background", pickHighContrastBrightColor},
	    {"base0B - Strings, Inherited Class, Markup Code, Diff Inserted",
	     pickHighContrastBrightColor},
	    {"base0C - Support, Regular Expressions, Escape Characters, Markup Quotes",
	     pickHighContrastBrightColor},
	    {"base0D - Functions, Methods, Attribute IDs, Headings", pickHighContrastBrightColor},
	    {"base0E - Keywords, Storage, Selector, Markup Italic, Diff Changed",
	     pickHighContrastBrightColor},
	    {"base0F - Deprecated, Opening/Closing Embedded Language Tags, e.g. <?php ?>",
	     pickHighContrastBrightColor}};
	for (int i = 0; (i < numColorsInPalette); ++i)
	{
		AutoColorFloat colorToFloat = autoColorCharToFloat(colorPalette[i]);
		workSpace[i] = autoColorRgbToHsl(colorToFloat);
	}
	qsort(workSpace, numColorsInPalette, sizeof(workSpace[0]),
	      autoColorSortHslColorFloatDarkestFirst);
	if (gAutoColorShouldPrint)
	{
		fprintf(stderr, "\nColors by lightness, darkest first:\n");
	}
	for (int i = 0; (i < numColorsInPalette); ++i)
	{
		AutoColorFloat colorRgb = autoColorHslToRgb(workSpace[i]);
		AutoColorStruct colorCharRgb = autoColorFloatToChar(colorRgb);
		if (gAutoColorShouldPrint)
		{
			fprintf(stderr, "#%02x%02x%02x\t\t%f lightness (hsl %f %f %f)\n", colorCharRgb.x,
			        colorCharRgb.y, colorCharRgb.z, workSpace[i].z, workSpace[i].x, workSpace[i].y,
			        workSpace[i].z);
		}
	}
	if (gAutoColorShouldPrint)
	{
		fprintf(stderr, "\n");
	}
	typedef struct ColorSelectionState
	{
		int nextIndex;
		unsigned char numRepeatUses;
		unsigned char numColorsThisMethod;
	} ColorSelectionState;
	ColorSelectionState darkColorState = {0, 0, 0};
	ColorSelectionState darkForegroundColorState = {(numColorsInPalette / 2), 0, 0};
	ColorSelectionState lightForegroundColorState = {(numColorsInPalette - 1), 0, 0};

	{
		for (int currentBase = 0;
		     (currentBase < (sizeof(selectionMethods) / sizeof(selectionMethods[0])));
		     ++currentBase)
		{
			AutoColorSelectionMethod selectionMethod = selectionMethods[currentBase].method;
			if ((pickDarkestColorForceDarkThreshold == selectionMethod))
			{
				++darkColorState.numColorsThisMethod;
			}
			else if ((pickDarkestHighContrastColor == selectionMethod))
			{
				++darkForegroundColorState.numColorsThisMethod;
			}
			else if ((pickHighContrastBrightColor == selectionMethod))
			{
				++lightForegroundColorState.numColorsThisMethod;
			}
		}
	}
	float backgroundLightness = -1.f;

	{
		for (int currentBase = 0;
		     (currentBase < (sizeof(selectionMethods) / sizeof(selectionMethods[0])));
		     ++currentBase)
		{
			AutoColorSelectionMethod selectionMethod = selectionMethods[currentBase].method;
			if ((pickDarkestColorForceDarkThreshold == selectionMethod))
			{
				AutoColorFloat clampedColor = workSpace[darkColorState.nextIndex];
				if (darkColorState.numRepeatUses)
				{
					clampedColor.z =
					    (maximumBackgroundBrightnessThresholds[darkColorState.nextIndex] *
					     (darkColorState.numRepeatUses /
					      ((float)darkColorState.numColorsThisMethod)));
				}
				clampedColor.z =
				    ((clampedColor.z <
				      maximumBackgroundBrightnessThresholds[darkColorState.nextIndex]) ?
				         clampedColor.z :
				         maximumBackgroundBrightnessThresholds[darkColorState.nextIndex]);
				if ((-1.f == backgroundLightness))
				{
					backgroundLightness = clampedColor.z;
				}
				AutoColorStruct darkColor = autoColorFloatToChar(autoColorHslToRgb(clampedColor));
				memcpy(base16ColorsOut[currentBase], (&darkColor), sizeof(AutoColor));
				++darkColorState.nextIndex;
				if ((darkColorState.nextIndex >= numColorsInPalette))
				{
					++darkColorState.numRepeatUses;
					darkColorState.nextIndex = (numColorsInPalette - 1);
				}
			}
			else if ((pickDarkestHighContrastColor == selectionMethod))
			{
				AutoColorFloat clampedColor = workSpace[darkForegroundColorState.nextIndex];
				if (darkForegroundColorState.numRepeatUses)
				{
					clampedColor.z = (minimumDeEmphasizedTextContrast +
					                  ((maximumTextContrast - minimumDeEmphasizedTextContrast) *
					                   (darkForegroundColorState.numRepeatUses /
					                    ((float)darkForegroundColorState.numColorsThisMethod))));
				}
				clampedColor = autoColorClampWithinContrastRange(clampedColor, backgroundLightness,
				                                                 minimumDeEmphasizedTextContrast,
				                                                 maximumTextContrast);
				AutoColorStruct deEmphasizedColor =
				    autoColorFloatToChar(autoColorHslToRgb(clampedColor));
				memcpy(base16ColorsOut[currentBase], (&deEmphasizedColor), sizeof(AutoColor));
				++darkForegroundColorState.nextIndex;
				if ((darkForegroundColorState.nextIndex >= numColorsInPalette))
				{
					++darkForegroundColorState.numRepeatUses;
					darkForegroundColorState.nextIndex = (numColorsInPalette - 1);
				}
			}
			else if ((pickHighContrastBrightColor == selectionMethod))
			{
				AutoColorFloat clampedColor = workSpace[lightForegroundColorState.nextIndex];
				if (lightForegroundColorState.numRepeatUses)
				{
					clampedColor.z = (minimumTextContrast +
					                  ((maximumTextContrast - minimumTextContrast) *
					                   (lightForegroundColorState.numRepeatUses /
					                    ((float)lightForegroundColorState.numColorsThisMethod))));
				}
				clampedColor = autoColorClampWithinContrastRange(
				    clampedColor, backgroundLightness, minimumTextContrast, maximumTextContrast);
				AutoColorStruct foregroundColor =
				    autoColorFloatToChar(autoColorHslToRgb(clampedColor));
				memcpy(base16ColorsOut[currentBase], (&foregroundColor), sizeof(AutoColor));
				if ((lightForegroundColorState.nextIndex > 0))
				{
					--lightForegroundColorState.nextIndex;
				}
				else
				{
					++lightForegroundColorState.numRepeatUses;
				}
			}
			if (gAutoColorShouldPrint)
			{
				fprintf(stderr, "#%02x%02x%02x\t\t%s\n", base16ColorsOut[currentBase][0],
				        base16ColorsOut[currentBase][1], base16ColorsOut[currentBase][2],
				        selectionMethods[currentBase].description);
			}
		}
	}
}

bool autoColorPickFromCurrentBackground(AutoColor* base16ColorsOut)
{
	const char* errorString = NULL;
	char backgroundFilename[1024] = {0};
	if (!(autoColorGetCurrentBackgroundFilename(backgroundFilename, sizeof(backgroundFilename),
	                                            (&errorString))))
	{
		if (gAutoColorShouldPrint)
		{
			fprintf(stderr, "error: %s", errorString);
		}
		return false;
	}
	if (gAutoColorShouldPrint)
	{
		fprintf(stderr, "\nPicking colors from '%s'\n", backgroundFilename);
	}
	AutoColorImage imageData = {0};
	if (!(autoColorLoadImage(backgroundFilename, (&imageData))))
	{
		return false;
	}
	AutoColor colorPalette[16];
	unsigned char numColorsRequested = (sizeof(colorPalette) / sizeof(colorPalette[0]));
	unsigned char numColorsAttained =
	    autoColorPickColorsByThreshold((&imageData), colorPalette, numColorsRequested);
	for (int i = 0; (i < numColorsAttained); ++i)
	{
		if (gAutoColorShouldPrint)
		{
			fprintf(stderr, "#%02x%02x%02x\n", colorPalette[i][0], colorPalette[i][1],
			        colorPalette[i][2]);
		}
	}
	AutoColorFloat workSpace[16];
	autoColorCreateBase16ThemeFromColors(colorPalette, numColorsAttained, workSpace,
	                                     base16ColorsOut);
	autoColorImageDestroy((&imageData));
	return true;
}

/*
The following code was copied and modified by Macoy Madson from the following library, which has the
following license:

uriparser - RFC 3986 URI parsing library

Copyright (C) 2007, Weijia Song <songweijia@gmail.com>
Copyright (C) 2007, Sebastian Pipping <sebastian@pipping.org>
All rights reserved.

Redistribution and use in source  and binary forms, with or without
modification, are permitted provided  that the following conditions
are met:

    1. Redistributions  of  source  code   must  retain  the  above
       copyright notice, this list  of conditions and the following
       disclaimer.

    2. Redistributions  in binary  form  must  reproduce the  above
       copyright notice, this list  of conditions and the following
       disclaimer  in  the  documentation  and/or  other  materials
       provided with the distribution.

    3. Neither the  name of the  copyright holder nor the  names of
       its contributors may be used  to endorse or promote products
       derived from  this software  without specific  prior written
       permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND  ANY EXPRESS OR IMPLIED WARRANTIES,  INCLUDING, BUT NOT
LIMITED TO,  THE IMPLIED WARRANTIES OF  MERCHANTABILITY AND FITNESS
FOR  A  PARTICULAR  PURPOSE  ARE  DISCLAIMED.  IN  NO  EVENT  SHALL
THE  COPYRIGHT HOLDER  OR CONTRIBUTORS  BE LIABLE  FOR ANY  DIRECT,
INDIRECT, INCIDENTAL, SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA,  OR PROFITS; OR BUSINESS INTERRUPTION)
HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT  LIABILITY,  OR  TORT (INCLUDING  NEGLIGENCE  OR  OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
OF THE POSSIBILITY OF SUCH DAMAGE.*/

unsigned char uriHexdigToInt(char hexdig)
{
	switch (hexdig)
	{
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
			return (unsigned char)(9 + hexdig - '9');

		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
			return (unsigned char)(15 + hexdig - 'f');

		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
			return (unsigned char)(15 + hexdig - 'F');

		default:
			return 0;
	}
}

const char* uriUnescapeInPlaceEx(char* inout)
{
	char* read = inout;
	char* write = inout;
	bool prevWasCr = false;

	typedef enum UriBreakConversionEnum
	{
		URI_BR_TO_LF,                       /**< Convert to Unix line breaks ("\\x0a") */
		URI_BR_TO_CRLF,                     /**< Convert to Windows line breaks ("\\x0d\\x0a") */
		URI_BR_TO_CR,                       /**< Convert to Macintosh line breaks ("\\x0d") */
		URI_BR_TO_UNIX = URI_BR_TO_LF,      /**< @copydoc UriBreakConversionEnum::URI_BR_TO_LF */
		URI_BR_TO_WINDOWS = URI_BR_TO_CRLF, /**< @copydoc UriBreakConversionEnum::URI_BR_TO_CRLF */
		URI_BR_TO_MAC = URI_BR_TO_CR,       /**< @copydoc UriBreakConversionEnum::URI_BR_TO_CR */
		URI_BR_DONT_TOUCH                   /**< Copy line breaks unmodified */
	} UriBreakConversion;                   /**< @copydoc UriBreakConversionEnum */

	bool plusToSpace = false;
	UriBreakConversion breakConversion = URI_BR_DONT_TOUCH;

	if (!inout)
	{
		return 0;
	}

	for (;;)
	{
		switch (read[0])
		{
			case '\0':
				if (read > write)
				{
					write[0] = '\0';
				}
				return write;

			case '%':
				switch (read[1])
				{
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case 'a':
					case 'b':
					case 'c':
					case 'd':
					case 'e':
					case 'f':
					case 'A':
					case 'B':
					case 'C':
					case 'D':
					case 'E':
					case 'F':
						switch (read[2])
						{
							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							case '8':
							case '9':
							case 'a':
							case 'b':
							case 'c':
							case 'd':
							case 'e':
							case 'f':
							case 'A':
							case 'B':
							case 'C':
							case 'D':
							case 'E':
							case 'F':
							{
								/* Percent group found */
								const unsigned char left = uriHexdigToInt(read[1]);
								const unsigned char right = uriHexdigToInt(read[2]);
								const int code = 16 * left + right;
								switch (code)
								{
									case 10:
										switch (breakConversion)
										{
											case URI_BR_TO_LF:
												if (!prevWasCr)
												{
													write[0] = (char)10;
													write++;
												}
												break;

											case URI_BR_TO_CRLF:
												if (!prevWasCr)
												{
													write[0] = (char)13;
													write[1] = (char)10;
													write += 2;
												}
												break;

											case URI_BR_TO_CR:
												if (!prevWasCr)
												{
													write[0] = (char)13;
													write++;
												}
												break;

											case URI_BR_DONT_TOUCH:
											default:
												write[0] = (char)10;
												write++;
										}
										prevWasCr = false;
										break;

									case 13:
										switch (breakConversion)
										{
											case URI_BR_TO_LF:
												write[0] = (char)10;
												write++;
												break;

											case URI_BR_TO_CRLF:
												write[0] = (char)13;
												write[1] = (char)10;
												write += 2;
												break;

											case URI_BR_TO_CR:
												write[0] = (char)13;
												write++;
												break;

											case URI_BR_DONT_TOUCH:
											default:
												write[0] = (char)13;
												write++;
										}
										prevWasCr = true;
										break;

									default:
										write[0] = (char)(code);
										write++;

										prevWasCr = false;
								}
								read += 3;
							}
							break;

							default:
								/* Copy two chars unmodified and */
								/* look at this char again */
								if (read > write)
								{
									write[0] = read[0];
									write[1] = read[1];
								}
								read += 2;
								write += 2;

								prevWasCr = false;
						}
						break;

					default:
						/* Copy one char unmodified and */
						/* look at this char again */
						if (read > write)
						{
							write[0] = read[0];
						}
						read++;
						write++;

						prevWasCr = false;
				}
				break;

			case '+':
				if (plusToSpace)
				{
					/* Convert '+' to ' ' */
					write[0] = ' ';
				}
				else
				{
					/* Copy one char unmodified */
					if (read > write)
					{
						write[0] = read[0];
					}
				}
				read++;
				write++;

				prevWasCr = false;
				break;

			default:
				/* Copy one char unmodified */
				if (read > write)
				{
					write[0] = read[0];
				}
				read++;
				write++;

				prevWasCr = false;
		}
	}
}
