
% Execution Code

		%----------------- Write float subroutine -----------------
floatwrite		 nop		% Start of the float write subroutine
		sw -36(r14),r1		% Save contents of r1
		sw -40(r14),r2		% Save contents of r2
		sw -44(r14),r3		% Save contents of r3
		sw -48(r14),r4		% Save contents of r4
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
		lw r15,-52(r14)		% Restore the return address
		jr r15		% Return from the float write subroutine

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

		%==================== Function/Method: bubbleSort ====================

bubbleSort		sw -24(r14),r15			% Tag the function call address

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
		sw -8(r14),r0		% Declare the variable n
		sw -12(r14),r0		% Declare the variable i
		sw -16(r14),r0		% Declare the variable j
		sw -20(r14),r0		% Declare the variable temp
		addi r14,r14,-8		% Load Data Member: n

		addi r12,r14,0		% Load the location of the variable n (r14)

		subi r14,r14,-8		% Unload Data Member
		addi r14,r14,-4		% Load Data Member: size

		addi r11,r14,0		% Load the location of the variable size (r14)

		subi r14,r14,-4		% Unload Data Member

		lw r11,0(r11)		% Get the value to assign to the data member
		sw 0(r12),r11		% Assign Data Member


		addi r14,r14,-12		% Load Data Member: i

		addi r12,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member

		addi r11,r0,0		% Load the integer value 0 into r11
		sw 0(r12),r11		% Assign Data Member


		addi r14,r14,-16		% Load Data Member: j

		addi r12,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r11,r0,0		% Load the integer value 0 into r11
		sw 0(r12),r11		% Assign Data Member


		addi r14,r14,-20		% Load Data Member: temp

		addi r12,r14,0		% Load the location of the variable temp (r14)

		subi r14,r14,-20		% Unload Data Member

		addi r11,r0,0		% Load the integer value 0 into r11
		sw 0(r12),r11		% Assign Data Member


gowhile1		 nop		% Go to the while loop
		addi r14,r14,-12		% Load Data Member: i

		addi r12,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member
		addi r14,r14,-8		% Load Data Member: n

		addi r11,r14,0		% Load the location of the variable n (r14)

		subi r14,r14,-8		% Unload Data Member

		addi r10,r0,1		% Load the integer value 1 into r10
		lw r11,0(r11)		% Load the value of r11
		sub r9,r11,r10		% - the values
		lw r12,0(r12)		% Load the value of r12
		clt r10,r12,r9		% < the values
		bz r10,endwhile1		% Check the while condition
		addi r14,r14,-16		% Load Data Member: j

		addi r10,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r9,r0,0		% Load the integer value 0 into r9
		sw 0(r10),r9		% Assign Data Member


gowhile2		 nop		% Go to the while loop
		addi r14,r14,-16		% Load Data Member: j

		addi r10,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member
		addi r14,r14,-8		% Load Data Member: n

		addi r9,r14,0		% Load the location of the variable n (r14)

		subi r14,r14,-8		% Unload Data Member
		addi r14,r14,-12		% Load Data Member: i

		addi r12,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member
		lw r9,0(r9)		% Load the value of r9
		lw r12,0(r12)		% Load the value of r12
		sub r11,r9,r12		% - the values

		addi r12,r0,1		% Load the integer value 1 into r12
		sub r9,r11,r12		% - the values
		lw r10,0(r10)		% Load the value of r10
		clt r12,r10,r9		% < the values
		bz r12,endwhile2		% Check the while condition
		addi r14,r14,0		% Load Data Member: arr
		addi r14,r14,-16		% Load Data Member: j

		addi r12,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r9,r14,0		% Load the location of the variable arr (r14)
		lw r9,0(r9)		% Load the location of the variable arr 
		lw r12,0(r12)		% Load the index value
		muli r12,r12,4		% Multiply the index by the element size
		sub r9,r9,r12		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member
		addi r14,r14,0		% Load Data Member: arr
		addi r14,r14,-16		% Load Data Member: j

		addi r12,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r10,r0,1		% Load the integer value 1 into r10
		lw r12,0(r12)		% Load the value of r12
		add r11,r12,r10		% + the values

		addi r10,r14,0		% Load the location of the variable arr (r14)
		lw r10,0(r10)		% Load the location of the variable arr 
		muli r11,r11,4		% Multiply the index by the element size
		sub r10,r10,r11		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member
		lw r9,0(r9)		% Load the value of r9
		lw r10,0(r10)		% Load the value of r10
		cgt r11,r9,r10		% > the values
ifthen1		bz r11,else1		% Check the if condition
		addi r14,r14,-20		% Load Data Member: temp

		addi r11,r14,0		% Load the location of the variable temp (r14)

		subi r14,r14,-20		% Unload Data Member
		addi r14,r14,0		% Load Data Member: arr
		addi r14,r14,-16		% Load Data Member: j

		addi r10,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r9,r14,0		% Load the location of the variable arr (r14)
		lw r9,0(r9)		% Load the location of the variable arr 
		lw r10,0(r10)		% Load the index value
		muli r10,r10,4		% Multiply the index by the element size
		sub r9,r9,r10		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		lw r9,0(r9)		% Get the value to assign to the data member
		sw 0(r11),r9		% Assign Data Member


		addi r14,r14,0		% Load Data Member: arr
		addi r14,r14,-16		% Load Data Member: j

		addi r11,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r9,r14,0		% Load the location of the variable arr (r14)
		lw r9,0(r9)		% Load the location of the variable arr 
		lw r11,0(r11)		% Load the index value
		muli r11,r11,4		% Multiply the index by the element size
		sub r9,r9,r11		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member
		addi r14,r14,0		% Load Data Member: arr
		addi r14,r14,-16		% Load Data Member: j

		addi r11,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r10,r0,1		% Load the integer value 1 into r10
		lw r11,0(r11)		% Load the value of r11
		add r12,r11,r10		% + the values

		addi r10,r14,0		% Load the location of the variable arr (r14)
		lw r10,0(r10)		% Load the location of the variable arr 
		muli r12,r12,4		% Multiply the index by the element size
		sub r10,r10,r12		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		lw r10,0(r10)		% Get the value to assign to the data member
		sw 0(r9),r10		% Assign Data Member


		addi r14,r14,0		% Load Data Member: arr
		addi r14,r14,-16		% Load Data Member: j

		addi r9,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r10,r0,1		% Load the integer value 1 into r10
		lw r9,0(r9)		% Load the value of r9
		add r12,r9,r10		% + the values

		addi r10,r14,0		% Load the location of the variable arr (r14)
		lw r10,0(r10)		% Load the location of the variable arr 
		muli r12,r12,4		% Multiply the index by the element size
		sub r10,r10,r12		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member
		addi r14,r14,-20		% Load Data Member: temp

		addi r12,r14,0		% Load the location of the variable temp (r14)

		subi r14,r14,-20		% Unload Data Member

		lw r12,0(r12)		% Get the value to assign to the data member
		sw 0(r10),r12		% Assign Data Member


		j endif1		% Jump to the end of the if statement
else1		 nop		% Else statement
endif1		 nop		% End of the if statement
		addi r14,r14,-16		% Load Data Member: j

		addi r10,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member
		addi r14,r14,-16		% Load Data Member: j

		addi r12,r14,0		% Load the location of the variable j (r14)

		subi r14,r14,-16		% Unload Data Member

		addi r9,r0,1		% Load the integer value 1 into r9
		lw r12,0(r12)		% Load the value of r12
		add r11,r12,r9		% + the values
		sw 0(r10),r11		% Assign Data Member


		j gowhile2		% Go to the while loop
endwhile2		 nop		% End of the while loop
		addi r14,r14,-12		% Load Data Member: i

		addi r10,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member
		addi r14,r14,-12		% Load Data Member: i

		addi r11,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member

		addi r9,r0,1		% Load the integer value 1 into r9
		lw r11,0(r11)		% Load the value of r11
		add r12,r11,r9		% + the values
		sw 0(r10),r12		% Assign Data Member


		j gowhile1		% Go to the while loop
endwhile1		 nop		% End of the while loop

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
		lw r15,-24(r14)			% Jump back to the return address
		jr r15

		%==================== End of bubbleSort ====================


		%==================== Function/Method: printArray ====================

printArray		sw -16(r14),r15			% Tag the function call address

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
		sw -8(r14),r0		% Declare the variable n
		sw -12(r14),r0		% Declare the variable i
		addi r14,r14,-8		% Load Data Member: n

		addi r10,r14,0		% Load the location of the variable n (r14)

		subi r14,r14,-8		% Unload Data Member
		addi r14,r14,-4		% Load Data Member: size

		addi r12,r14,0		% Load the location of the variable size (r14)

		subi r14,r14,-4		% Unload Data Member

		lw r12,0(r12)		% Get the value to assign to the data member
		sw 0(r10),r12		% Assign Data Member


		addi r14,r14,-12		% Load Data Member: i

		addi r10,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member

		addi r12,r0,0		% Load the integer value 0 into r12
		sw 0(r10),r12		% Assign Data Member


gowhile5		 nop		% Go to the while loop
		addi r14,r14,-12		% Load Data Member: i

		addi r10,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member
		addi r14,r14,-8		% Load Data Member: n

		addi r12,r14,0		% Load the location of the variable n (r14)

		subi r14,r14,-8		% Unload Data Member
		lw r10,0(r10)		% Load the value of r10
		lw r12,0(r12)		% Load the value of r12
		clt r9,r10,r12		% < the values
		bz r9,endwhile5		% Check the while condition
		addi r14,r14,0		% Load Data Member: arr
		addi r14,r14,-12		% Load Data Member: i

		addi r9,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member

		addi r12,r14,0		% Load the location of the variable arr (r14)
		lw r12,0(r12)		% Load the location of the variable arr 
		lw r9,0(r9)		% Load the index value
		muli r9,r9,4		% Multiply the index by the element size
		sub r12,r12,r9		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		%----------------- WRITE Integer -----------------
		addi r14,r14,-80		% Move to the next stack frame
		lw r12,0(r12)		% Get the integer value to write
		sw -28(r14),r12
		jl r15,intwrite		% Call the integer write subroutine
		addi r14,r14,80		% Move back to the current stack frame

		addi r14,r14,-12		% Load Data Member: i

		addi r12,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member
		addi r14,r14,-12		% Load Data Member: i

		addi r9,r14,0		% Load the location of the variable i (r14)

		subi r14,r14,-12		% Unload Data Member

		addi r10,r0,1		% Load the integer value 1 into r10
		lw r9,0(r9)		% Load the value of r9
		add r11,r9,r10		% + the values
		sw 0(r12),r11		% Assign Data Member


		j gowhile5		% Go to the while loop
endwhile5		 nop		% End of the while loop

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
		lw r15,-16(r14)			% Jump back to the return address
		jr r15

		%==================== End of printArray ====================


		%==================== Function/Method: main ====================

entry		% Start of the program
		addi r14,r0,topaddr		% Set the top of the stack
		sw 0(r14),r0		% Declare the variable arr
		addi r14,r14,0		% Load Data Member: arr

		addi r12,r0,0		% Load the integer value 0 into r12

		addi r11,r14,0		% Load the location of the variable arr (r14)
		muli r12,r12,4		% Multiply the index by the element size
		sub r11,r11,r12		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r12,r0,64		% Load the integer value 64 into r12
		sw 0(r11),r12		% Assign Data Member


		addi r14,r14,0		% Load Data Member: arr

		addi r11,r0,1		% Load the integer value 1 into r11

		addi r12,r14,0		% Load the location of the variable arr (r14)
		muli r11,r11,4		% Multiply the index by the element size
		sub r12,r12,r11		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r11,r0,34		% Load the integer value 34 into r11
		sw 0(r12),r11		% Assign Data Member


		addi r14,r14,0		% Load Data Member: arr

		addi r12,r0,2		% Load the integer value 2 into r12

		addi r11,r14,0		% Load the location of the variable arr (r14)
		muli r12,r12,4		% Multiply the index by the element size
		sub r11,r11,r12		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r12,r0,25		% Load the integer value 25 into r12
		sw 0(r11),r12		% Assign Data Member


		addi r14,r14,0		% Load Data Member: arr

		addi r11,r0,3		% Load the integer value 3 into r11

		addi r12,r14,0		% Load the location of the variable arr (r14)
		muli r11,r11,4		% Multiply the index by the element size
		sub r12,r12,r11		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r11,r0,12		% Load the integer value 12 into r11
		sw 0(r12),r11		% Assign Data Member


		addi r14,r14,0		% Load Data Member: arr

		addi r12,r0,4		% Load the integer value 4 into r12

		addi r11,r14,0		% Load the location of the variable arr (r14)
		muli r12,r12,4		% Multiply the index by the element size
		sub r11,r11,r12		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r12,r0,22		% Load the integer value 22 into r12
		sw 0(r11),r12		% Assign Data Member


		addi r14,r14,0		% Load Data Member: arr

		addi r11,r0,5		% Load the integer value 5 into r11

		addi r12,r14,0		% Load the location of the variable arr (r14)
		muli r11,r11,4		% Multiply the index by the element size
		sub r12,r12,r11		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r11,r0,11		% Load the integer value 11 into r11
		sw 0(r12),r11		% Assign Data Member


		addi r14,r14,0		% Load Data Member: arr

		addi r12,r0,6		% Load the integer value 6 into r12

		addi r11,r14,0		% Load the location of the variable arr (r14)
		muli r12,r12,4		% Multiply the index by the element size
		sub r11,r11,r12		% Load memory location of the array arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r12,r0,90		% Load the integer value 90 into r12
		sw 0(r11),r12		% Assign Data Member


		subi r14,r14,0
		addi r14,r14,0		% Load Data Member: arr

		addi r11,r14,0		% Load the location of the variable arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r12,r0,7		% Load the integer value 7 into r12
		addi r14,r14,0

		%----------------- Function Call: main -> printArray -----------------
		sw -88(r14),r12		% Pass param size
		sw -84(r14),r11		% Pass param arr reference

		addi r14,r14,-84		% Load the function stack frame
		jl r15,printArray		% Jump to the function printArray
		addi r14,r14,84		% Restore the stack frame
		subi r14,r14,0
		addi r14,r14,0		% Load Data Member: arr

		addi r11,r14,0		% Load the location of the variable arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r12,r0,7		% Load the integer value 7 into r12
		addi r14,r14,0

		%----------------- Function Call: main -> bubbleSort -----------------
		sw -88(r14),r12		% Pass param size
		sw -84(r14),r11		% Pass param arr reference

		addi r14,r14,-84		% Load the function stack frame
		jl r15,bubbleSort		% Jump to the function bubbleSort
		addi r14,r14,84		% Restore the stack frame
		subi r14,r14,0
		addi r14,r14,0		% Load Data Member: arr

		addi r11,r14,0		% Load the location of the variable arr (r14)

		subi r14,r14,0		% Unload Data Member

		addi r12,r0,7		% Load the integer value 7 into r12
		addi r14,r14,0

		%----------------- Function Call: main -> printArray -----------------
		sw -88(r14),r12		% Pass param size
		sw -84(r14),r11		% Pass param arr reference

		addi r14,r14,-84		% Load the function stack frame
		jl r15,printArray		% Jump to the function printArray
		addi r14,r14,84		% Restore the stack frame
hlt		% Halt the program


		%==================== End of main ====================


% Data Section
nl		db 13, 10, 0
dot		db ".", 0
entint		db "Enter an integer: ", 0
entfloat		db "Enter a float: ", 0
buf		res 100
