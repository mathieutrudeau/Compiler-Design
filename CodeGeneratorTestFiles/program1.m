
% Execution Code
entry
		addi r14,r0,topaddr
		sw -20(r14),r0 		% Initializing a to 0 (Default Value)
		lw r12,-20(r14) 		% Loading a into r12
		addi r11,r0,4		% Loading 4 into r11
		add r12,r0,r11		% Assigning r11 to r12
		sw -20(r14),r12
		lw r12,-20(r14) 		% Loading a into r12
		addi r14,r0, topaddr		% Load the top address of the stack
		sw -8(r14),r12
		addi r12,r0,buf		% Put the address on the buffer stack
		sw -12(r14),r12
		jl r15, intstr		% Call the int to string subroutine
		sw -8(r14),r13		% Copy the result to the stack
		jl r15, putstr		% Call the print string subroutine
		addi r12,r0,nl		% Load the newline character
		sw -8(r14),r12
		jl r15, putstr		% Call the print string subroutine
hlt

% Data Section
buf		res 24
nl		db 13, 10, 0
