
% Execution Code

		%----------------- Write float subroutine -----------------
floatwrite		 nop		% Start of the float write subroutine
		sw -36(r14),r1		% Save contents of r1
		sw -40(r14),r2		% Save contents of r2
		sw -44(r14),r3		% Save contents of r3
		sw -48(r14),r4		% Save contents of r4
		sw -56(r14),r5		% Save contents of r5
		sw -52(r14),r15		% Save the return address
		lw r1,-28(r14)		% Load the float value
		lw r2,-32(r14)		% Load the point position
		addi r3,r0,1		% Initialize the modulus divisor
whilemodulus		ceq r4,r2,r0		% Check if the point position is 0
		bnz r4,endwhilemodulus		% If the point position is 0, exit the loop
			muli r3,r3,10		% Multiply the modulus divisor by 10
			subi r2,r2,1		% Decrement the point position
			bz r2,endwhilemodulus		% If the point position is 0, exit the loop
			j whilemodulus		% Jump back to the start of the loop
endwhilemodulus		 nop		% End of the while loop
		mod r4,r1,r3		% Calculate the integer part of the float value
		sub r1,r1,r4		% Remove the fractional part of the float value
		div r1,r1,r3		% Divide the integer part by the modulus divisor
		add r2,r0,r4		% Save the fractional part of the float value

		sw -8(r14),r1		% Store the integer part of the float value
		addi r2,r0,buf		% Load the buffer address
		sw -12(r14),r2		% Store the buffer address
		jl r15,intstr		% Call the int -> string subroutine
		sw -8(r14),r13		% Store the string address
		jl r15,putstr		% Call the print subroutine

		addi r2,r0,dot		% Load the decimal point
		sw -8(r14),r2		% Store the decimal point
		jl r15,putstr		% Call the print subroutine

		clt r2,r4,r0		% Check if the fractional part is negative
		bnz r2,negfrac		% If the fractional part is negative, jump to negfrac
showfrac		sw -8(r14),r4		% Store the fractional part of the float value
		addi r2,r0,buf		% Load the buffer address
		sw -12(r14),r2		% Store the buffer address
		jl r15,intstr		% Call the int -> string subroutine
		sw -8(r14),r13		% Store the string address
		jl r15,lenstr		% Call the length of string subroutine
		lw r2,-32(r14)		% Load the point position
		sub r5,r2,r13		% Calculate the length of the fractional part
whileleadingzero		cgti r2,r5,0		% Load the point position
		bz r2,endwhileleadingzero		% If the length of the fractional part is not less than the point position, exit the loop
			addi r2,r0,zero		% Load the zero character
			sw -8(r14),r2		% Store the zero character
			jl r15,putstr		% Call the print subroutine
			subi r5,r5,1		% Decrement the length of the fractional part
			j whileleadingzero		% Jump back to the start of the loop
endwhileleadingzero
		sw -8(r14),r4		% Store the fractional part of the float value
		addi r2,r0,buf		% Load the buffer address
		sw -12(r14),r2		% Store the buffer address
		jl r15,intstr		% Call the int -> string subroutine
		sw -8(r14),r13		% Store the string address
		jl r15,putstr		% Call the print subroutine

		addi r4,r0,nl		% Load the newline
		sw -8(r14),r4		% Store the newline
		jl r15,putstr		% Call the print subroutine
		lw r1,-36(r14)		% Restore contents of r1
		lw r2,-40(r14)		% Restore contents of r2
		lw r3,-44(r14)		% Restore contents of r3
		lw r4,-48(r14)		% Restore contents of r4
		lw r5,-56(r14)		% Restore contents of r5
		lw r15,-52(r14)		% Restore the return address
		jr r15		% Return from the float write subroutine

negfrac		muli r4,r4,-1		% Make the fractional part positive
		j showfrac		% Jump to showfrac

		%----------------- Write integer subroutine -----------------

intwrite		 nop		% Start of the integer write subroutine
		sw -36(r14),r1		% Save contents of r1
		sw -40(r14),r2		% Save contents of r2
		sw -44(r14),r3		% Save contents of r3
		sw -48(r14),r4		% Save contents of r4
		sw -52(r14),r15		% Save the return address
		lw r1,-28(r14)		% Load the integer value

		sw -8(r14),r1		% Store the integer value
		addi r2,r0,buf		% Load the buffer address
		sw -12(r14),r2		% Store the buffer address
		jl r15,intstr		% Call the int -> string subroutine
		sw -8(r14),r13		% Store the string address
		jl r15,putstr		% Call the print subroutine

		addi r4,r0,nl		% Load the newline
		sw -8(r14),r4		% Store the newline
		jl r15,putstr		% Call the print subroutine
		lw r1,-36(r14)		% Restore contents of r1
		lw r2,-40(r14)		% Restore contents of r2
		lw r3,-44(r14)		% Restore contents of r3
		lw r4,-48(r14)		% Restore contents of r4
		lw r15,-52(r14)		% Restore the return address
		jr r15		% Return from the integer write subroutine

		%----------------- Read float subroutine -----------------
getfloat		 nop		% Start of the float read subroutine
		sw -36(r14),r1		% Save contents of r1
		sw -40(r14),r2		% Save contents of r2
		sw -44(r14),r3		% Save contents of r3
		sw -48(r14),r4		% Save contents of r4
		sw -52(r14),r15		% Save the return address
		addi r4,r0,0		% Initialize the point position
		addi r1,r0,buf		% Load the buffer address

getfloat1		getc r2		% Get the next character
		ceqi r3,r2,46		% Check if the character is a decimal point
		bnz r3,getfloat2		% If the character is not a decimal point, jump to getfloat2
		ceqi r3,r2,10		% Check if the character is a newline
		bnz r3,endgetfloat		% If the character is a newline, jump to endgetfloat
		sb 0(r1),r2		% Store the character in the buffer
		addi r1,r1,1		% Increment the buffer address
		j getfloat1		% Get the next character

getfloat2		getc r2		% Get the next character
		ceqi r3,r2,10		% Check if the character is a newline
		bnz r3,endgetfloat		% If the character is a newline, jump to endgetfloat
		addi r4,r4,1		% Increment the point position
		sb 0(r1),r2		% Store the character in the buffer
		addi r1,r1,1		% Increment the buffer address
		j getfloat2		% Get the next character

endgetfloat		 sb 0(r1),r0		% Add a null terminator to the buffer
		sw -56(r14),r4		% Store the point position
		jl r15,strint		% Call the string -> int subroutine
		sw -60(r14),r13		% Store the integer part of the float value
		lw r1,-36(r14)		% Restore contents of r1
		lw r2,-40(r14)		% Restore contents of r2
		lw r3,-44(r14)		% Restore contents of r3
		lw r4,-48(r14)		% Restore contents of r4
		lw r15,-52(r14)		% Restore the return address
		jr r15		% Return from the float read subroutine

		%----------------- add Float Subroutine -----------------
addfloat		 nop		% Start of the add float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
addfloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,addfloat2		% If the point positions are equal, jump to addfloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,addfloat3		% If the first point position is less than the second point position, jump to addfloat3
		j addfloat4		% Jump to addfloat4
addfloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j addfloat1		% Jump to addfloat1
addfloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j addfloat1		% Jump to addfloat1
addfloat2		add r15,r1,r3		% Perform the add operation on the float values
		sw 0(r14),r15		% Store the result of the add operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the add float subroutine

		%----------------- mul Float Subroutine -----------------
mulfloat		 nop		% Start of the mul float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
		mul r15,r1,r3		% Perform the mul operation on the float values
		add r2,r2,r4		% Add the point positions
		sw 0(r14),r15		% Store the result of the mul operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the mul float subroutine

		%----------------- div Float Subroutine -----------------
divfloat		 nop		% Start of the div float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
divfloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,divfloat2		% If the point positions are equal, jump to divfloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,divfloat3		% If the first point position is less than the second point position, jump to divfloat3
		j divfloat4		% Jump to divfloat4
divfloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j divfloat1		% Jump to divfloat1
divfloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j divfloat1		% Jump to divfloat1
divfloat2		div r15,r1,r3		% Perform the div operation on the float values
		sw 0(r14),r15		% Store the result of the div operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the div float subroutine

		%----------------- sub Float Subroutine -----------------
subfloat		 nop		% Start of the sub float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
subfloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,subfloat2		% If the point positions are equal, jump to subfloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,subfloat3		% If the first point position is less than the second point position, jump to subfloat3
		j subfloat4		% Jump to subfloat4
subfloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j subfloat1		% Jump to subfloat1
subfloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j subfloat1		% Jump to subfloat1
subfloat2		sub r15,r1,r3		% Perform the sub operation on the float values
		sw 0(r14),r15		% Store the result of the sub operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the sub float subroutine

		%----------------- clt Float Subroutine -----------------
cltfloat		 nop		% Start of the clt float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
cltfloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,cltfloat2		% If the point positions are equal, jump to cltfloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,cltfloat3		% If the first point position is less than the second point position, jump to cltfloat3
		j cltfloat4		% Jump to cltfloat4
cltfloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j cltfloat1		% Jump to cltfloat1
cltfloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j cltfloat1		% Jump to cltfloat1
cltfloat2		clt r15,r1,r3		% Perform the clt operation on the float values
		sw 0(r14),r15		% Store the result of the clt operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the clt float subroutine

		%----------------- cle Float Subroutine -----------------
clefloat		 nop		% Start of the cle float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
clefloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,clefloat2		% If the point positions are equal, jump to clefloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,clefloat3		% If the first point position is less than the second point position, jump to clefloat3
		j clefloat4		% Jump to clefloat4
clefloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j clefloat1		% Jump to clefloat1
clefloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j clefloat1		% Jump to clefloat1
clefloat2		cle r15,r1,r3		% Perform the cle operation on the float values
		sw 0(r14),r15		% Store the result of the cle operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the cle float subroutine

		%----------------- cgt Float Subroutine -----------------
cgtfloat		 nop		% Start of the cgt float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
cgtfloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,cgtfloat2		% If the point positions are equal, jump to cgtfloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,cgtfloat3		% If the first point position is less than the second point position, jump to cgtfloat3
		j cgtfloat4		% Jump to cgtfloat4
cgtfloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j cgtfloat1		% Jump to cgtfloat1
cgtfloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j cgtfloat1		% Jump to cgtfloat1
cgtfloat2		cgt r15,r1,r3		% Perform the cgt operation on the float values
		sw 0(r14),r15		% Store the result of the cgt operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the cgt float subroutine

		%----------------- cge Float Subroutine -----------------
cgefloat		 nop		% Start of the cge float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
cgefloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,cgefloat2		% If the point positions are equal, jump to cgefloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,cgefloat3		% If the first point position is less than the second point position, jump to cgefloat3
		j cgefloat4		% Jump to cgefloat4
cgefloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j cgefloat1		% Jump to cgefloat1
cgefloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j cgefloat1		% Jump to cgefloat1
cgefloat2		cge r15,r1,r3		% Perform the cge operation on the float values
		sw 0(r14),r15		% Store the result of the cge operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the cge float subroutine

		%----------------- ceq Float Subroutine -----------------
ceqfloat		 nop		% Start of the ceq float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
ceqfloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,ceqfloat2		% If the point positions are equal, jump to ceqfloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,ceqfloat3		% If the first point position is less than the second point position, jump to ceqfloat3
		j ceqfloat4		% Jump to ceqfloat4
ceqfloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j ceqfloat1		% Jump to ceqfloat1
ceqfloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j ceqfloat1		% Jump to ceqfloat1
ceqfloat2		ceq r15,r1,r3		% Perform the ceq operation on the float values
		sw 0(r14),r15		% Store the result of the ceq operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the ceq float subroutine

		%----------------- cne Float Subroutine -----------------
cnefloat		 nop		% Start of the cne float subroutine
		sw -16(r14),r1		% Save contents of r1
		sw -20(r14),r2		% Save contents of r2
		sw -24(r14),r3		% Save contents of r3
		sw -28(r14),r4		% Save contents of r4
		sw -32(r14),r15		% Save the return address
		lw r1,0(r14)		% Load the first float value
		lw r2,-4(r14)		% Load the point position of the first float value
		lw r3,-8(r14)		% Load the second float value
		lw r4,-12(r14)		% Load the point position of the second float value
cnefloat1		ceq r15,r2,r4		% Check if the point positions are equal
		bnz r15,cnefloat2		% If the point positions are equal, jump to cnefloat2
		clt r15,r2,r4		% Check if the first point position is less than the second point position
		bnz r15,cnefloat3		% If the first point position is less than the second point position, jump to cnefloat3
		j cnefloat4		% Jump to cnefloat4
cnefloat3		addi r2,r2,1		% Increment the first point position
		muli r1,r1,10		% Multiply the first float value by 10
		j cnefloat1		% Jump to cnefloat1
cnefloat4		addi r4,r4,1		% Increment the second point position
		muli r3,r3,10		% Multiply the second float value by 10
		j cnefloat1		% Jump to cnefloat1
cnefloat2		cne r15,r1,r3		% Perform the cne operation on the float values
		sw 0(r14),r15		% Store the result of the cne operation
		sw -4(r14),r2		% Store the point position of the result
		lw r1,-16(r14)		% Restore contents of r1
		lw r2,-20(r14)		% Restore contents of r2
		lw r3,-24(r14)		% Restore contents of r3
		lw r4,-28(r14)		% Restore contents of r4
		lw r15,-32(r14)		% Restore the return address
		jr r15		% Return from the cne float subroutine

		%----------------- Mult Float Subroutine -----------------

		%----------------- Div Float Subroutine -----------------

		%==================== Function/Method: evaluate ====================

evaluate_POLYNOMIAL		sw -8(r14),r15			% Tag the function call address

		%----------------- Save Buffer -----------------
		sw -24(r14),r1		% Save buffer register r1
		sw -28(r14),r2		% Save buffer register r2
		sw -32(r14),r3		% Save buffer register r3
		sw -36(r14),r4		% Save buffer register r4
		sw -40(r14),r5		% Save buffer register r5
		sw -44(r14),r6		% Save buffer register r6
		sw -48(r14),r7		% Save buffer register r7
		sw -52(r14),r8		% Save buffer register r8
		sw -56(r14),r9		% Save buffer register r9
		sw -60(r14),r10		% Save buffer register r10
		sw -64(r14),r11		% Save buffer register r11
		sw -68(r14),r12		% Save buffer register r12
		sw -72(r14),r13		% Save buffer register r13
		sw -76(r14),r14		% Save buffer register r14
		sw -80(r14),r15		% Save buffer register r15

		addi r12,r0,0		% Load the integer value 00 into r12
		addi r11,r0,1		% Load the point position of the float value 00 into r11
		sw -12(r14),r11		% Store the return value
		sw -16(r14),r12		% Store the point position of the float value

		%----------------- Restore Buffer -----------------
		lw r1,-24(r14)		% Save buffer register r1
		lw r2,-28(r14)		% Save buffer register r2
		lw r3,-32(r14)		% Save buffer register r3
		lw r4,-36(r14)		% Save buffer register r4
		lw r5,-40(r14)		% Save buffer register r5
		lw r6,-44(r14)		% Save buffer register r6
		lw r7,-48(r14)		% Save buffer register r7
		lw r8,-52(r14)		% Save buffer register r8
		lw r9,-56(r14)		% Save buffer register r9
		lw r10,-60(r14)		% Save buffer register r10
		lw r11,-64(r14)		% Save buffer register r11
		lw r12,-68(r14)		% Save buffer register r12
		lw r13,-72(r14)		% Save buffer register r13
		lw r14,-76(r14)		% Save buffer register r14
		lw r15,-80(r14)		% Save buffer register r15
		lw r15,-8(r14)			% Jump back to the return address
		jr r15

		%==================== End of evaluate ====================


		%==================== Function/Method: evaluate ====================

evaluate_QUADRATIC		sw -16(r14),r15			% Tag the function call address

		%----------------- Save Buffer -----------------
		sw -32(r14),r1		% Save buffer register r1
		sw -36(r14),r2		% Save buffer register r2
		sw -40(r14),r3		% Save buffer register r3
		sw -44(r14),r4		% Save buffer register r4
		sw -48(r14),r5		% Save buffer register r5
		sw -52(r14),r6		% Save buffer register r6
		sw -56(r14),r7		% Save buffer register r7
		sw -60(r14),r8		% Save buffer register r8
		sw -64(r14),r9		% Save buffer register r9
		sw -68(r14),r10		% Save buffer register r10
		sw -72(r14),r11		% Save buffer register r11
		sw -76(r14),r12		% Save buffer register r12
		sw -80(r14),r13		% Save buffer register r13
		sw -84(r14),r14		% Save buffer register r14
		sw -88(r14),r15		% Save buffer register r15
		sw -8(r14),r0		% Declare the variable result
		addi r14,r14,-8		% Load Data Member: result

		addi r11,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member

		lw r12,-28(r14)		% Load the class reference this
		addi r12,r12,0		% Load the location of the variable a: <|DATA|>

		lw r10,-4(r12)		% Get the point position of the float value

		lw r12,0(r12)		% Get the value to assign to the data member
		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r10		% Assign Data Member: Point Position


		addi r14,r14,-8		% Load Data Member: result

		addi r11,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member
		addi r14,r14,-8		% Load Data Member: result

		addi r12,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member
		addi r14,r14,0		% Load Data Member: x

		addi r10,r14,0		% Load the location of the variable x (r14)

		subi r14,r14,0		% Unload Data Member

		%----------------- Mult Float Expression: * -----------------
		lw r9,-4(r10)		% Get the point position of the float value
		lw r10,0(r10)		% Get the value to negate
		lw r8,-4(r12)		% Get the point position of the float value
		lw r12,0(r12)		% Get the value to negate
		addi r14,r14,-88		% Move to the next stack frame
		sw 0(r14),r12		% Store the float value
		sw -4(r14),r8		% Store the point position
		sw -8(r14),r10		% Store the float value
		sw -12(r14),r9		% Store the point position
		jl r15,mulfloat		% Call the float mult subroutine
		lw r8,0(r14)		% Load the result
		lw r12,-4(r14)		% Load the point position
		addi r8,r8,0
		addi r12,r12,0
		addi r14,r14,88		% Move back to the current stack frame


		lw r9,-28(r14)		% Load the class reference this
		addi r9,r9,-8		% Load the location of the variable b: <|DATA|>

		%----------------- Add Float Expression: + -----------------
		lw r10,-4(r9)		% Get the point position of the float value
		lw r9,0(r9)		% Get the value
		addi r14,r14,-88		% Move to the next stack frame
		sw 0(r14),r8		% Store the float value
		sw -4(r14),r12		% Store the point position
		sw -8(r14),r9		% Store the float value
		sw -12(r14),r10		% Store the point position
		jl r15,addfloat		% Call the float compare subroutine
		lw r12,0(r14)		% Load the result
		lw r8,-4(r14)		% Load the point position
		addi r12,r12,0
		addi r8,r8,0
		addi r14,r14,88		% Move back to the current stack frame

		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r8		% Assign Data Member: Point Position


		addi r14,r14,-8		% Load Data Member: result

		addi r11,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member
		addi r14,r14,-8		% Load Data Member: result

		addi r12,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member
		addi r14,r14,0		% Load Data Member: x

		addi r8,r14,0		% Load the location of the variable x (r14)

		subi r14,r14,0		% Unload Data Member

		%----------------- Mult Float Expression: * -----------------
		lw r10,-4(r8)		% Get the point position of the float value
		lw r8,0(r8)		% Get the value to negate
		lw r9,-4(r12)		% Get the point position of the float value
		lw r12,0(r12)		% Get the value to negate
		addi r14,r14,-88		% Move to the next stack frame
		sw 0(r14),r12		% Store the float value
		sw -4(r14),r9		% Store the point position
		sw -8(r14),r8		% Store the float value
		sw -12(r14),r10		% Store the point position
		jl r15,mulfloat		% Call the float mult subroutine
		lw r9,0(r14)		% Load the result
		lw r12,-4(r14)		% Load the point position
		addi r9,r9,0
		addi r12,r12,0
		addi r14,r14,88		% Move back to the current stack frame


		lw r10,-28(r14)		% Load the class reference this
		addi r10,r10,-16		% Load the location of the variable c: <|DATA|>

		%----------------- Add Float Expression: + -----------------
		lw r8,-4(r10)		% Get the point position of the float value
		lw r10,0(r10)		% Get the value
		addi r14,r14,-88		% Move to the next stack frame
		sw 0(r14),r9		% Store the float value
		sw -4(r14),r12		% Store the point position
		sw -8(r14),r10		% Store the float value
		sw -12(r14),r8		% Store the point position
		jl r15,addfloat		% Call the float compare subroutine
		lw r12,0(r14)		% Load the result
		lw r9,-4(r14)		% Load the point position
		addi r12,r12,0
		addi r9,r9,0
		addi r14,r14,88		% Move back to the current stack frame

		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r9		% Assign Data Member: Point Position


		addi r14,r14,-8		% Load Data Member: result

		addi r11,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member
		lw r12,-4(r11)		% Load the point position of the float value
		lw r11,0(r11)		% Load the value of r11
		sw -20(r14),r11		% Store the return value
		sw -24(r14),r12		% Store the point position of the float value

		%----------------- Restore Buffer -----------------
		lw r1,-32(r14)		% Save buffer register r1
		lw r2,-36(r14)		% Save buffer register r2
		lw r3,-40(r14)		% Save buffer register r3
		lw r4,-44(r14)		% Save buffer register r4
		lw r5,-48(r14)		% Save buffer register r5
		lw r6,-52(r14)		% Save buffer register r6
		lw r7,-56(r14)		% Save buffer register r7
		lw r8,-60(r14)		% Save buffer register r8
		lw r9,-64(r14)		% Save buffer register r9
		lw r10,-68(r14)		% Save buffer register r10
		lw r11,-72(r14)		% Save buffer register r11
		lw r12,-76(r14)		% Save buffer register r12
		lw r13,-80(r14)		% Save buffer register r13
		lw r14,-84(r14)		% Save buffer register r14
		lw r15,-88(r14)		% Save buffer register r15
		lw r15,-16(r14)			% Jump back to the return address
		jr r15

		%==================== End of evaluate ====================


		%==================== Function/Method: build ====================

build_QUADRATIC		sw -48(r14),r15			% Tag the function call address

		%----------------- Save Buffer -----------------
		sw -60(r14),r1		% Save buffer register r1
		sw -64(r14),r2		% Save buffer register r2
		sw -68(r14),r3		% Save buffer register r3
		sw -72(r14),r4		% Save buffer register r4
		sw -76(r14),r5		% Save buffer register r5
		sw -80(r14),r6		% Save buffer register r6
		sw -84(r14),r7		% Save buffer register r7
		sw -88(r14),r8		% Save buffer register r8
		sw -92(r14),r9		% Save buffer register r9
		sw -96(r14),r10		% Save buffer register r10
		sw -100(r14),r11		% Save buffer register r11
		sw -104(r14),r12		% Save buffer register r12
		sw -108(r14),r13		% Save buffer register r13
		sw -112(r14),r14		% Save buffer register r14
		sw -116(r14),r15		% Save buffer register r15
		sw -24(r14),r0		% Declare the variable new_function
		addi r14,r14,-24		% Load Data Member: new_function

		addi r11,r14,0		% Load the location of the variable a (r14)

		subi r14,r14,-24		% Unload Data Member
		addi r14,r14,0		% Load Data Member: A

		addi r12,r14,0		% Load the location of the variable A (r14)

		subi r14,r14,0		% Unload Data Member

		lw r9,-4(r12)		% Get the point position of the float value

		lw r12,0(r12)		% Get the value to assign to the data member
		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r9		% Assign Data Member: Point Position


		addi r14,r14,-24		% Load Data Member: new_function

		addi r11,r14,0		% Load the location of the variable b (r14)

		subi r14,r14,-32		% Unload Data Member
		addi r14,r14,-8		% Load Data Member: B

		addi r12,r14,0		% Load the location of the variable B (r14)

		subi r14,r14,-8		% Unload Data Member

		lw r9,-4(r12)		% Get the point position of the float value

		lw r12,0(r12)		% Get the value to assign to the data member
		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r9		% Assign Data Member: Point Position


		addi r14,r14,-24		% Load Data Member: new_function

		addi r11,r14,0		% Load the location of the variable c (r14)

		subi r14,r14,-40		% Unload Data Member
		addi r14,r14,-16		% Load Data Member: C

		addi r12,r14,0		% Load the location of the variable C (r14)

		subi r14,r14,-16		% Unload Data Member

		lw r9,-4(r12)		% Get the point position of the float value

		lw r12,0(r12)		% Get the value to assign to the data member
		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r9		% Assign Data Member: Point Position


		addi r14,r14,-24		% Load Data Member: new_function

		addi r11,r14,0		% Load the location of the variable new_function (r14)

		subi r14,r14,-24		% Unload Data Member
		sw -52(r14),r11		% Store the return value

		%----------------- Restore Buffer -----------------
		lw r1,-60(r14)		% Save buffer register r1
		lw r2,-64(r14)		% Save buffer register r2
		lw r3,-68(r14)		% Save buffer register r3
		lw r4,-72(r14)		% Save buffer register r4
		lw r5,-76(r14)		% Save buffer register r5
		lw r6,-80(r14)		% Save buffer register r6
		lw r7,-84(r14)		% Save buffer register r7
		lw r8,-88(r14)		% Save buffer register r8
		lw r9,-92(r14)		% Save buffer register r9
		lw r10,-96(r14)		% Save buffer register r10
		lw r11,-100(r14)		% Save buffer register r11
		lw r12,-104(r14)		% Save buffer register r12
		lw r13,-108(r14)		% Save buffer register r13
		lw r14,-112(r14)		% Save buffer register r14
		lw r15,-116(r14)		% Save buffer register r15
		lw r15,-48(r14)			% Jump back to the return address
		jr r15

		%==================== End of build ====================


		%==================== Function/Method: build ====================

build_LINEAR		sw -32(r14),r15			% Tag the function call address

		%----------------- Save Buffer -----------------
		sw -44(r14),r1		% Save buffer register r1
		sw -48(r14),r2		% Save buffer register r2
		sw -52(r14),r3		% Save buffer register r3
		sw -56(r14),r4		% Save buffer register r4
		sw -60(r14),r5		% Save buffer register r5
		sw -64(r14),r6		% Save buffer register r6
		sw -68(r14),r7		% Save buffer register r7
		sw -72(r14),r8		% Save buffer register r8
		sw -76(r14),r9		% Save buffer register r9
		sw -80(r14),r10		% Save buffer register r10
		sw -84(r14),r11		% Save buffer register r11
		sw -88(r14),r12		% Save buffer register r12
		sw -92(r14),r13		% Save buffer register r13
		sw -96(r14),r14		% Save buffer register r14
		sw -100(r14),r15		% Save buffer register r15
		sw -16(r14),r0		% Declare the variable new_function
		addi r14,r14,-16		% Load Data Member: new_function

		addi r11,r14,0		% Load the location of the variable a (r14)

		subi r14,r14,-16		% Unload Data Member
		addi r14,r14,0		% Load Data Member: A

		addi r12,r14,0		% Load the location of the variable A (r14)

		subi r14,r14,0		% Unload Data Member

		lw r9,-4(r12)		% Get the point position of the float value

		lw r12,0(r12)		% Get the value to assign to the data member
		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r9		% Assign Data Member: Point Position


		addi r14,r14,-16		% Load Data Member: new_function

		addi r11,r14,0		% Load the location of the variable b (r14)

		subi r14,r14,-24		% Unload Data Member
		addi r14,r14,-8		% Load Data Member: B

		addi r12,r14,0		% Load the location of the variable B (r14)

		subi r14,r14,-8		% Unload Data Member

		lw r9,-4(r12)		% Get the point position of the float value

		lw r12,0(r12)		% Get the value to assign to the data member
		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r9		% Assign Data Member: Point Position


		addi r14,r14,-16		% Load Data Member: new_function

		addi r11,r14,0		% Load the location of the variable new_function (r14)

		subi r14,r14,-16		% Unload Data Member
		sw -36(r14),r11		% Store the return value

		%----------------- Restore Buffer -----------------
		lw r1,-44(r14)		% Save buffer register r1
		lw r2,-48(r14)		% Save buffer register r2
		lw r3,-52(r14)		% Save buffer register r3
		lw r4,-56(r14)		% Save buffer register r4
		lw r5,-60(r14)		% Save buffer register r5
		lw r6,-64(r14)		% Save buffer register r6
		lw r7,-68(r14)		% Save buffer register r7
		lw r8,-72(r14)		% Save buffer register r8
		lw r9,-76(r14)		% Save buffer register r9
		lw r10,-80(r14)		% Save buffer register r10
		lw r11,-84(r14)		% Save buffer register r11
		lw r12,-88(r14)		% Save buffer register r12
		lw r13,-92(r14)		% Save buffer register r13
		lw r14,-96(r14)		% Save buffer register r14
		lw r15,-100(r14)		% Save buffer register r15
		lw r15,-32(r14)			% Jump back to the return address
		jr r15

		%==================== End of build ====================


		%==================== Function/Method: evaluate ====================

evaluate_LINEAR		sw -16(r14),r15			% Tag the function call address

		%----------------- Save Buffer -----------------
		sw -32(r14),r1		% Save buffer register r1
		sw -36(r14),r2		% Save buffer register r2
		sw -40(r14),r3		% Save buffer register r3
		sw -44(r14),r4		% Save buffer register r4
		sw -48(r14),r5		% Save buffer register r5
		sw -52(r14),r6		% Save buffer register r6
		sw -56(r14),r7		% Save buffer register r7
		sw -60(r14),r8		% Save buffer register r8
		sw -64(r14),r9		% Save buffer register r9
		sw -68(r14),r10		% Save buffer register r10
		sw -72(r14),r11		% Save buffer register r11
		sw -76(r14),r12		% Save buffer register r12
		sw -80(r14),r13		% Save buffer register r13
		sw -84(r14),r14		% Save buffer register r14
		sw -88(r14),r15		% Save buffer register r15
		sw -8(r14),r0		% Declare the variable result
		addi r14,r14,-8		% Load Data Member: result

		addi r11,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member

		addi r12,r0,0		% Load the integer value 00 into r12
		addi r9,r0,1		% Load the point position of the float value 00 into r9
		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r9		% Assign Data Member: Point Position


		addi r14,r14,-8		% Load Data Member: result

		addi r11,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member

		lw r12,-28(r14)		% Load the class reference this
		addi r12,r12,0		% Load the location of the variable a: <|DATA|>
		addi r14,r14,0		% Load Data Member: x

		addi r9,r14,0		% Load the location of the variable x (r14)

		subi r14,r14,0		% Unload Data Member

		%----------------- Mult Float Expression: * -----------------
		lw r8,-4(r9)		% Get the point position of the float value
		lw r9,0(r9)		% Get the value to negate
		lw r10,-4(r12)		% Get the point position of the float value
		lw r12,0(r12)		% Get the value to negate
		addi r14,r14,-88		% Move to the next stack frame
		sw 0(r14),r12		% Store the float value
		sw -4(r14),r10		% Store the point position
		sw -8(r14),r9		% Store the float value
		sw -12(r14),r8		% Store the point position
		jl r15,mulfloat		% Call the float mult subroutine
		lw r10,0(r14)		% Load the result
		lw r12,-4(r14)		% Load the point position
		addi r10,r10,0
		addi r12,r12,0
		addi r14,r14,88		% Move back to the current stack frame


		lw r8,-28(r14)		% Load the class reference this
		addi r8,r8,-8		% Load the location of the variable b: <|DATA|>

		%----------------- Add Float Expression: + -----------------
		lw r9,-4(r8)		% Get the point position of the float value
		lw r8,0(r8)		% Get the value
		addi r14,r14,-88		% Move to the next stack frame
		sw 0(r14),r10		% Store the float value
		sw -4(r14),r12		% Store the point position
		sw -8(r14),r8		% Store the float value
		sw -12(r14),r9		% Store the point position
		jl r15,addfloat		% Call the float compare subroutine
		lw r12,0(r14)		% Load the result
		lw r10,-4(r14)		% Load the point position
		addi r12,r12,0
		addi r10,r10,0
		addi r14,r14,88		% Move back to the current stack frame

		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r10		% Assign Data Member: Point Position


		addi r14,r14,-8		% Load Data Member: result

		addi r11,r14,0		% Load the location of the variable result (r14)

		subi r14,r14,-8		% Unload Data Member
		lw r12,-4(r11)		% Load the point position of the float value
		lw r11,0(r11)		% Load the value of r11
		sw -20(r14),r11		% Store the return value
		sw -24(r14),r12		% Store the point position of the float value

		%----------------- Restore Buffer -----------------
		lw r1,-32(r14)		% Save buffer register r1
		lw r2,-36(r14)		% Save buffer register r2
		lw r3,-40(r14)		% Save buffer register r3
		lw r4,-44(r14)		% Save buffer register r4
		lw r5,-48(r14)		% Save buffer register r5
		lw r6,-52(r14)		% Save buffer register r6
		lw r7,-56(r14)		% Save buffer register r7
		lw r8,-60(r14)		% Save buffer register r8
		lw r9,-64(r14)		% Save buffer register r9
		lw r10,-68(r14)		% Save buffer register r10
		lw r11,-72(r14)		% Save buffer register r11
		lw r12,-76(r14)		% Save buffer register r12
		lw r13,-80(r14)		% Save buffer register r13
		lw r14,-84(r14)		% Save buffer register r14
		lw r15,-88(r14)		% Save buffer register r15
		lw r15,-16(r14)			% Jump back to the return address
		jr r15

		%==================== End of evaluate ====================


		%==================== Function/Method: main ====================

entry		% Start of the program
		addi r14,r0,topaddr		% Set the top of the stack
		sw 0(r14),r0		% Declare the variable f1
		sw -16(r14),r0		% Declare the variable f2
		sw -40(r14),r0		% Declare the variable counter
		addi r14,r14,0		% Load Data Member: f1

		addi r11,r14,0		% Load the location of the variable f1 (r14)

		subi r14,r14,0		% Unload Data Member
		addi r14,r14,0		% Load Data Member: f1
		subi r14,r14,0

		addi r12,r0,20		% Load the integer value 20 into r12
		addi r10,r0,1		% Load the point position of the float value 20 into r10

		addi r9,r0,35		% Load the integer value 35 into r9
		addi r8,r0,1		% Load the point position of the float value 35 into r8
		addi r14,r14,0

		%----------------- Function Call: main -> build -----------------
		sw -116(r14),r9		% Pass param B
		sw -120(r14),r8		% Pass param B point position
		sw -108(r14),r12		% Pass param A
		sw -112(r14),r10		% Pass param A point position
		sw -148(r14),r14		% Set the pointer to the current class instance

		addi r14,r14,-108		% Load the function stack frame
		jl r15,build_LINEAR		% Jump to the function build_LINEAR
		addi r14,r14,108		% Restore the stack frame
		lw r12,-144(r14)		% Get the return value

		subi r14,r14,0		% Unload Data Member

		%----------------- Assign LINEAR -----------------

		addi r11,r11,0		% Update the location register for the data member a
		addi r12,r12,0		% Update the location register for the value a
		lw r10,0(r12)		% Load the value of the data member a
		sw 0(r11),r10		% Assign Data Member: a
		lw r10,-4(r12)		% Load the point position of the data member a
		sw -4(r11),r10		% Assign Data Member: a

		addi r11,r11,-8		% Update the location register for the data member b
		addi r12,r12,-8		% Update the location register for the value b
		lw r10,0(r12)		% Load the value of the data member b
		sw 0(r11),r10		% Assign Data Member: b
		lw r10,-4(r12)		% Load the point position of the data member b
		sw -4(r11),r10		% Assign Data Member: b

		addi r14,r14,-16		% Load Data Member: f2

		addi r11,r14,0		% Load the location of the variable f2 (r14)

		subi r14,r14,-16		% Unload Data Member
		addi r14,r14,-16		% Load Data Member: f2
		subi r14,r14,-16

		addi r12,r0,20		% Load the integer value 20 into r12
		addi r10,r0,1		% Load the point position of the float value 20 into r10
		muli r12,r12,-1		% Negate the value

		addi r9,r0,10		% Load the integer value 10 into r9
		addi r8,r0,1		% Load the point position of the float value 10 into r8

		addi r7,r0,0		% Load the integer value 00 into r7
		addi r6,r0,1		% Load the point position of the float value 00 into r6
		addi r14,r14,-16

		%----------------- Function Call: main -> build -----------------
		sw -108(r14),r7		% Pass param C
		sw -112(r14),r6		% Pass param C point position
		sw -100(r14),r9		% Pass param B
		sw -104(r14),r8		% Pass param B point position
		sw -92(r14),r12		% Pass param A
		sw -96(r14),r10		% Pass param A point position
		sw -148(r14),r14		% Set the pointer to the current class instance

		addi r14,r14,-92		% Load the function stack frame
		jl r15,build_QUADRATIC		% Jump to the function build_QUADRATIC
		addi r14,r14,92		% Restore the stack frame
		lw r12,-144(r14)		% Get the return value

		subi r14,r14,-16		% Unload Data Member

		%----------------- Assign QUADRATIC -----------------

		addi r11,r11,0		% Update the location register for the data member a
		addi r12,r12,0		% Update the location register for the value a
		lw r10,0(r12)		% Load the value of the data member a
		sw 0(r11),r10		% Assign Data Member: a
		lw r10,-4(r12)		% Load the point position of the data member a
		sw -4(r11),r10		% Assign Data Member: a

		addi r11,r11,-8		% Update the location register for the data member b
		addi r12,r12,-8		% Update the location register for the value b
		lw r10,0(r12)		% Load the value of the data member b
		sw 0(r11),r10		% Assign Data Member: b
		lw r10,-4(r12)		% Load the point position of the data member b
		sw -4(r11),r10		% Assign Data Member: b

		addi r11,r11,-8		% Update the location register for the data member c
		addi r12,r12,-8		% Update the location register for the value c
		lw r10,0(r12)		% Load the value of the data member c
		sw 0(r11),r10		% Assign Data Member: c
		lw r10,-4(r12)		% Load the point position of the data member c
		sw -4(r11),r10		% Assign Data Member: c

		addi r14,r14,-40		% Load Data Member: counter

		addi r11,r14,0		% Load the location of the variable counter (r14)

		subi r14,r14,-40		% Unload Data Member

		addi r12,r0,1		% Load the integer value 1 into r12
		sw 0(r11),r12		% Assign Data Member


		sw -44(r14),r0		% Declare the variable fee
		addi r14,r14,-44		% Load Data Member: fee

		addi r11,r14,0		% Load the location of the variable fee (r14)

		subi r14,r14,-44		% Unload Data Member
		addi r14,r14,0		% Load Data Member: f1
		subi r14,r14,0

		addi r12,r0,20		% Load the integer value 20 into r12
		addi r10,r0,1		% Load the point position of the float value 20 into r10
		addi r14,r14,0

		%----------------- Function Call: main -> evaluate -----------------
		sw -108(r14),r12		% Pass param x
		sw -112(r14),r10		% Pass param x point position
		sw -136(r14),r14		% Set the pointer to the current class instance

		addi r14,r14,-108		% Load the function stack frame
		jl r15,evaluate_LINEAR		% Jump to the function evaluate_LINEAR
		addi r14,r14,108		% Restore the stack frame
		lw r12,-128(r14)		% Get the return value
		lw r10,-132(r14)		% Get the point position
		addi r12,r12,0
		addi r10,r10,0

		subi r14,r14,0		% Unload Data Member
		sw 0(r11),r12		% Assign Data Member: Value
		sw -4(r11),r10		% Assign Data Member: Point Position


		addi r14,r14,-44		% Load Data Member: fee

		addi r11,r14,0		% Load the location of the variable fee (r14)

		subi r14,r14,-44		% Unload Data Member

		lw r12,-4(r11)		% Load the point position of the float value to write
		lw r11,0(r11)		% Load the float value to write

		%----------------- WRITE Float -----------------
		addi r14,r14,-108		% Move to the next stack frame
		sw -28(r14),r11		% Store the float value
		sw -32(r14),r12		% Store the point position
		jl r15,floatwrite		% Call the float write subroutine
		addi r14,r14,108		% Move back to the current stack frame

gowhile1		 nop		% Go to the while loop
		addi r14,r14,-40		% Load Data Member: counter

		addi r12,r14,0		% Load the location of the variable counter (r14)

		subi r14,r14,-40		% Unload Data Member

		addi r11,r0,10		% Load the integer value 10 into r11
		lw r12,0(r12)		% Load the value of r12
		cle r10,r12,r11		% <= the values
		bz r10,endwhile1		% Check the while condition
		addi r14,r14,-40		% Load Data Member: counter

		addi r10,r14,0		% Load the location of the variable counter (r14)

		subi r14,r14,-40		% Unload Data Member

		%----------------- WRITE Integer -----------------
		addi r14,r14,-108		% Move to the next stack frame
		lw r10,0(r10)		% Get the integer value to write
		sw -28(r14),r10
		jl r15,intwrite		% Call the integer write subroutine
		addi r14,r14,108		% Move back to the current stack frame

		addi r14,r14,0		% Load Data Member: f1
		subi r14,r14,0
		addi r14,r14,-40		% Load Data Member: counter

		addi r10,r14,0		% Load the location of the variable counter (r14)

		subi r14,r14,-40		% Unload Data Member
		addi r14,r14,0

		%----------------- Function Call: main -> evaluate -----------------
		lw r11,-4(r10)		% Get the point position of the float value
		lw r10,0(r10)		% Load param x value
		sw -108(r14),r10		% Pass param x
		sw -112(r14),r11		% Pass param x point position
		sw -136(r14),r14		% Set the pointer to the current class instance

		addi r14,r14,-108		% Load the function stack frame
		jl r15,evaluate_LINEAR		% Jump to the function evaluate_LINEAR
		addi r14,r14,108		% Restore the stack frame
		lw r10,-128(r14)		% Get the return value
		lw r11,-132(r14)		% Get the point position
		addi r10,r10,0
		addi r11,r11,0

		subi r14,r14,0		% Unload Data Member

		%----------------- WRITE Float -----------------
		addi r14,r14,-108		% Move to the next stack frame
		sw -28(r14),r10		% Store the float value
		sw -32(r14),r11		% Store the point position
		jl r15,floatwrite		% Call the float write subroutine
		addi r14,r14,108		% Move back to the current stack frame

		addi r14,r14,-16		% Load Data Member: f2
		subi r14,r14,-16
		addi r14,r14,-40		% Load Data Member: counter

		addi r11,r14,0		% Load the location of the variable counter (r14)

		subi r14,r14,-40		% Unload Data Member
		addi r14,r14,-16

		%----------------- Function Call: main -> evaluate -----------------
		lw r10,-4(r11)		% Get the point position of the float value
		lw r11,0(r11)		% Load param x value
		sw -92(r14),r11		% Pass param x
		sw -96(r14),r10		% Pass param x point position
		sw -120(r14),r14		% Set the pointer to the current class instance

		addi r14,r14,-92		% Load the function stack frame
		jl r15,evaluate_QUADRATIC		% Jump to the function evaluate_QUADRATIC
		addi r14,r14,92		% Restore the stack frame
		lw r11,-112(r14)		% Get the return value
		lw r10,-116(r14)		% Get the point position
		addi r11,r11,0
		addi r10,r10,0

		subi r14,r14,-16		% Unload Data Member

		%----------------- WRITE Float -----------------
		addi r14,r14,-108		% Move to the next stack frame
		sw -28(r14),r11		% Store the float value
		sw -32(r14),r10		% Store the point position
		jl r15,floatwrite		% Call the float write subroutine
		addi r14,r14,108		% Move back to the current stack frame

		addi r14,r14,-40		% Load Data Member: counter

		addi r10,r14,0		% Load the location of the variable counter (r14)

		subi r14,r14,-40		% Unload Data Member
		addi r14,r14,-40		% Load Data Member: counter

		addi r11,r14,0		% Load the location of the variable counter (r14)

		subi r14,r14,-40		% Unload Data Member

		addi r12,r0,1		% Load the integer value 1 into r12
		lw r11,0(r11)		% Load the value of r11
		add r9,r11,r12		% + the values
		sw 0(r10),r9		% Assign Data Member


		j gowhile1		% Go to the while loop
endwhile1		 nop		% End of the while loop
hlt		% Halt the program


		%==================== End of main ====================


% Data Section
nl		db 13, 10, 0
zero		db "0", 0
dot		db ".", 0
entint		db "Enter an integer: ", 0
entfloat		db "Enter a float: ", 0
buf		res 100
