
% Execution Code

		%==================== main ====================

entry		% Start of the program
		addi r14,r0,topaddr		% Set the top of the stack
		sw 0(r14),r0 		% Initializing a to 0 (Default Value)
		sw -4(r14),r0 		% Initializing b to 0 (Default Value)
		sw -8(r14),r0 		% Initializing c to 0 (Default Value)
		lw r12,0(r14) 		% Loading a into r12

		%----------------- READ -----------------
		addi r14,r14,-12			% Go to the next stack frame
		addi r11,r0,entint			% Prompt for an integer
		sw -8(r14),r11
		jl r15,putstr
		addi r11,r0,buf
		sw -8(r14),r11
		jl r15,getstr			% Call the get string subroutine
		jl r15,strint			% Call the string -> int subroutine
		addi r11,r13,0
		addi r14,r14,12			% Go back to the current stack frame

		add r12,r0,r11		% Assigning r11 to r12
		sw 0(r14),r12
		lw r12,-4(r14) 		% Loading b into r12

		%----------------- READ -----------------
		addi r14,r14,-12			% Go to the next stack frame
		addi r11,r0,entint			% Prompt for an integer
		sw -8(r14),r11
		jl r15,putstr
		addi r11,r0,buf
		sw -8(r14),r11
		jl r15,getstr			% Call the get string subroutine
		jl r15,strint			% Call the string -> int subroutine
		addi r11,r13,0
		addi r14,r14,12			% Go back to the current stack frame

		add r12,r0,r11		% Assigning r11 to r12
		sw -4(r14),r12
		lw r12,0(r14) 		% Loading a into r12
		lw r11,-4(r14) 		% Loading b into r11
		sw -12(r14),r11				% Storing the parameter a
		sw -16(r14),r12				% Storing the parameter b
		addi r14,r14,-12			% Increment the stack frame
		jl r15,f1					% Jump to the function f1
		addi r14,r14,12				% Decrement the stack frame
		lw r10,-8(r14) 		% Loading c into r10
		addi r9,r0,3		% Loading 3 into r9
		add r10,r0,r9		% Assigning r9 to r10
		sw -8(r14),r10
		lw r10,-8(r14) 		% Loading c into r10

		%----------------- WRITE -----------------
		addi r14,r14,-12		% Move to the next stack frame
		sw -8(r14),r10
		addi r10,r0,buf
		sw -12(r14),r10
		jl r15,intstr		% Call the int -> string subroutine
		sw -8(r14),r13
		jl r15,putstr		% Call the print subroutine
		addi r10,r0,nl
		sw -8(r14),r10
		jl r15,putstr		% Print a newline
		addi r14,r14,12		% Move back to the current stack frame

hlt		% Halt the program


		%==================== End of main ====================


		%==================== f1 ====================

f1		sw -16(r14),r15			% Tag the function call address
		sw -8(r14),r0 		% Initializing c to 0 (Default Value)
		lw r10,-8(r14) 		% Loading c into r10
		lw r9,0(r14) 		% Loading a into r9
		lw r8,-4(r14) 		% Loading b into r8
		add r7, r9, r8		% r9 + r8 = r7
		add r10,r0,r7		% Assigning r7 to r10
		sw -8(r14),r10
		lw r10,-8(r14) 		% Loading c into r10

		%----------------- WRITE -----------------
		addi r14,r14,-24		% Move to the next stack frame
		sw -8(r14),r10
		addi r10,r0,buf
		sw -12(r14),r10
		jl r15,intstr		% Call the int -> string subroutine
		sw -8(r14),r13
		jl r15,putstr		% Call the print subroutine
		addi r10,r0,nl
		sw -8(r14),r10
		jl r15,putstr		% Print a newline
		addi r14,r14,24		% Move back to the current stack frame

		lw r15,-16(r14)			% Jump back to the return address
		jr r15					% Jump back to the return address

		%==================== End of f1 ====================


% Data Section
nl		db 13, 10, 0
entint		db "Enter an integer: ", 0
buf		res 20
