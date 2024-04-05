
% Execution Code

		%==================== Function/Method: main ====================

entry		% Start of the program
		addi r14,r0,topaddr		% Set the top of the stack
		sw 0(r14),r0 		% Initializing a to 0 (Default Value)
		sw -4(r14),r0 		% Initializing b to 0 (Default Value)
		sw -8(r14),r0 		% Initializing c to 0 (Default Value)
		lw r12,0(r14) 		% Loading a into r12
		addi r11,r0,3		% Loading 3 into r11
		add r12,r0,r11		% Assigning r11 to r12
		sw 0(r14),r12
		lw r12,-4(r14) 		% Loading b into r12
		addi r11,r0,4		% Loading 4 into r11
		add r12,r0,r11		% Assigning r11 to r12
		sw -4(r14),r12
		lw r12,-8(r14) 		% Loading c into r12
		lw r11,0(r14) 		% Loading a into r11
		lw r10,-4(r14) 		% Loading b into r10

		add r9, r11, r10		% r11 + r10 = r9
		add r12,r0,r9		% Assigning r9 to r12
		sw -8(r14),r12
		lw r12,-8(r14) 		% Loading c into r12

		%----------------- WRITE -----------------
		addi r14,r14,-24		% Move to the next stack frame
		sw -8(r14),r12
		addi r12,r0,buf
		sw -12(r14),r12
		jl r15,intstr		% Call the int -> string subroutine
		sw -8(r14),r13
		jl r15,putstr		% Call the print subroutine
		addi r12,r0,nl
		sw -8(r14),r12
		jl r15,putstr		% Print a newline
		addi r14,r14,24		% Move back to the current stack frame

		sw -12(r14),r0 		% Initializing d to 0 (Default Value)
		lw r12,-12(r14) 		% Loading d into r12

		lw r11,-12(r14) 		% Loading a into r11
		addi r11,r11,1		% Incrementing r11 by 1
		sw -12(r14),r11		% Storing the return value

		addi r14,r14,-12		% Move to the next stack frame

		lw r12,0(r14) 		% Loading a into r12

		addi r14,r14,12		% Go back to the previous stack frame



		%----------------- WRITE -----------------
		addi r14,r14,-24		% Move to the next stack frame
		sw -8(r14),r12
		addi r12,r0,buf
		sw -12(r14),r12
		jl r15,intstr		% Call the int -> string subroutine
		sw -8(r14),r13
		jl r15,putstr		% Call the print subroutine
		addi r12,r0,nl
		sw -8(r14),r12
		jl r15,putstr		% Print a newline
		addi r14,r14,24		% Move back to the current stack frame

hlt		% Halt the program


		%==================== End of main ====================


		%==================== Function/Method: f1 ====================

f1		sw -8(r14),r15			% Tag the function call address
		sw 0(r14),r0 		% Initializing a to 0 (Default Value)
		lw r12,0(r14) 		% Loading a into r12
		sw -12(r14),r12			% Storing the return value
		lw r15,-8(r14)			% Jump back to the return address
		jr r15					% Jump back to the return address

		%==================== End of f1 ====================


% Data Section
nl		db 13, 10, 0
entint		db "Enter an integer: ", 0
buf		res 20
