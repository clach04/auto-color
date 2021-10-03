#include <stdio.h>

#include "AutoColor.h"

int main()
{
	AutoColor base16Colors[16] = {0};
	if (!autoColorPickFromCurrentBackground(base16Colors))
		return 1;
	fprintf(stderr, "The first color is RGB %d, %d, %d\n", base16Colors[0][0], base16Colors[0][1],
	        base16Colors[0][2]);
	return 0;
}
