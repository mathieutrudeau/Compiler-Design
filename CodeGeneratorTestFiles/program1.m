
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

		%==================== Function/Method: main ====================

entry		% Start of the program
		addi r14,r0,topaddr		% Set the top of the stack
		sw 0(r14),r0 		% Initializing a to 0 (Default Value)
		sw -8(r14),r0 		% Initializing b to 0 (Default Value)
		lw r12,0(r14) 		% Loading a into r12
		lw r11,-4(r14) 		% Loading the point position of a into r11

		% Loading Float Value: 33.22

		% Loading Integer Value: 3322
		addi r10,r0,0
		sl r10,8
		addi r10,r10,0
		sl r10,8
		addi r10,r10,12
		sl r10,8
		addi r10,r10,250
		addi r9,r0,2		% Load the point position of the float value

		% Assignment of Float Value
		add r12,r0,r10		% Assigning r10 to r12
		sw 0(r14),r12
		sw -4(r14),r9
		lw r11,0(r14) 		% Loading a into r11
		lw r12,-4(r14) 		% Loading the point position of a into r12

		%----------------- WRITE Float -----------------
		addi r14,r14,-12		% Move to the next stack frame
		sw -28(r14),r11			% Save contents of value
		sw -32(r14),r12			% Save contents of point position
		jl r15,floatwrite		% Call the float write subroutine
		addi r14,r14,12			% Move back to the current stack frame

		lw r12,0(r14) 		% Loading a into r12
		lw r11,-4(r14) 		% Loading the point position of a into r11

		%----------------- READ Float -----------------
		addi r14,r14,-12				% Go to the next stack frame
		addi r10,r0,entfloat			% Prompt for a float
		sw -8(r14),r10
		jl r15,putstr
		addi r10,r0,buf
		sw -8(r14),r10
		jl r15,getfloat			% Call the float read subroutine
		lw r10,-60(r14)			% Load the integer part of the float value
		lw r9,-56(r14)			% Load the point position of the float value
		addi r14,r14,12			% Go back to the current stack frame


		% Assignment of Float Value
		add r12,r0,r10		% Assigning r10 to r12
		sw 0(r14),r12
		sw -4(r14),r9
		lw r11,0(r14) 		% Loading a into r11
		lw r12,-4(r14) 		% Loading the point position of a into r12

		%----------------- WRITE Float -----------------
		addi r14,r14,-12		% Move to the next stack frame
		sw -28(r14),r11			% Save contents of value
		sw -32(r14),r12			% Save contents of point position
		jl r15,floatwrite		% Call the float write subroutine
		addi r14,r14,12			% Move back to the current stack frame

		addi r14,r14,-12			% Increment the stack frame
		jl r15,f1					% Jump to the function f1
		addi r14,r14,12				% Decrement the stack frame
		lw r12,-16(r14)				% Loading the return value
hlt		% Halt the program


		%==================== End of main ====================


		%==================== Function/Method: f1 ====================

f1		sw 0(r14),r15			% Tag the function call address
		lw r15,0(r14)			% Jump back to the return address
		jr r15					% Jump back to the return address

		%==================== End of f1 ====================


% Data Section
nl		db 13, 10, 0
dot		db ".", 0
entint		db "Enter an integer: ", 0
entfloat		db "Enter a float: ", 0
buf		res 100
