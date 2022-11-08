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

#include <stdbool.h>

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
