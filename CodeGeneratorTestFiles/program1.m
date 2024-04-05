
% Execution Code
entry
		addi r14,r0,topaddr
		%-------------------- main ----------------------
		sw -20(r14),r0 		% Initializing a to 0 (Default Value)
		sw -24(r14),r0 		% Initializing b to 0 (Default Value)
		sw -28(r14),r0 		% Initializing c to 0 (Default Value)
		lw r12,-20(r14) 		% Loading a into r12

		%----------------- READ -----------------
		addi r11,r0,entint
		sw -8(r14),r11
		jl r15, putstr		% Call the print string subroutine
		addi r11,r0,buf		% Get the address of the buffer
		sw -8(r14),r11
		jl r15, getstr		% Call the get string subroutine
		jl r15, strint		% Call the string to int subroutine
		addi r11,r13,0		% Copy the result to the register
		add r12,r0,r11		% Assigning r11 to r12
		sw -20(r14),r12
		lw r12,-24(r14) 		% Loading b into r12

		%----------------- READ -----------------
		addi r11,r0,entint
		sw -8(r14),r11
		jl r15, putstr		% Call the print string subroutine
		addi r11,r0,buf		% Get the address of the buffer
		sw -8(r14),r11
		jl r15, getstr		% Call the get string subroutine
		jl r15, strint		% Call the string to int subroutine
		addi r11,r13,0		% Copy the result to the register
		add r12,r0,r11		% Assigning r11 to r12
		sw -24(r14),r12
		lw r12,-28(r14) 		% Loading c into r12
		lw r11,-20(r14) 		% Loading a into r11
		lw r10,-24(r14) 		% Loading b into r10
		add r11,r0,r10		% Assigning r10 to r11
		sw -20(r14),r11
		lw r11,-28(r14) 		% Loading c into r11

		%----------------- WRITE -----------------
		addi r14,r0, topaddr		% Load the top address of the stack
		sw -8(r14),r11
		addi r11,r0,buf		% Put the address on the buffer stack
		sw -12(r14),r11
		jl r15, intstr		% Call the int to string subroutine
		sw -8(r14),r13		% Copy the result to the stack
		jl r15, putstr		% Call the print string subroutine
		addi r11,r0,nl		% Load the newline character
		sw -8(r14),r11
		jl r15, putstr		% Call the print string subroutine
		%-------------------- f1 ----------------------
		lw r11,-32(r14) 		% Loading a into r11
		lw r10,-36(r14) 		% Loading b into r10
		add r9, r11, r10		% r11 + r10 = r9
hlt

% Data Section
buf		res 20
nl		db 13, 10, 0
entint		db "Enter an integer: ", 0
