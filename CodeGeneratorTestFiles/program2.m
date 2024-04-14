
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

		%==================== Function/Method: main ====================

entry		% Start of the program
		addi r14,r0,topaddr		% Set the top of the stack

		addi r12,r0,0		% Load the integer value 3000003 into r12
		sl r12,8
		addi r12,r12,45
		sl r12,8
		addi r12,r12,198
		sl r12,8
		addi r12,r12,195
		addi r11,r0,6		% Load the point position of the float value 3000003 into r11
		muli r12,r12,-1		% Negate the value

		%----------------- WRITE Float -----------------
		addi r14,r14,-56		% Move to the next stack frame
		sw -28(r14),r12		% Store the float value
		sw -32(r14),r11		% Store the point position
		jl r15,floatwrite		% Call the float write subroutine
		addi r14,r14,56		% Move back to the current stack frame

hlt		% Halt the program


		%==================== End of main ====================


% Data Section
nl		db 13, 10, 0
zero		db "0", 0
dot		db ".", 0
entint		db "Enter an integer: ", 0
entfloat		db "Enter a float: ", 0
buf		res 100
