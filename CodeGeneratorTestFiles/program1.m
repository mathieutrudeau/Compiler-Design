
% Execution Code
entry
		addi r12,r0,2		% Loading 2 into r12
		addi r11,r0,1		% Loading 1 into r11
		cgt r12, r11, r10		% r12 > r11 = r10
ifthen1		bz r10,else1		% If r10 is false, jump to else1
		addi r10,r0,3		% Loading 3 into r10
		addi r11,r0,2		% Loading 2 into r11
		cgt r10, r11, r12		% r10 > r11 = r12
ifthen2		bz r12,else2		% If r12 is false, jump to else2
		lw r12,b_4(r0) 		% Loading b : integer
		addi r11,r0,4		% Loading 4 into r11
		add r12,r0,r11		% Assigning r11 to r12
		j endif2		% Jump to endif2
else2		 nop		% Start of the else block
endif2		 nop		% End of the if block
		j endif1		% Jump to endif1
else1		 nop		% Start of the else block
		lw r12,b_4(r0) 		% Loading b : integer
		addi r11,r0,10		% Loading 10 into r11
		add r12,r0,r11		% Assigning r11 to r12
endif1		 nop		% End of the if block
hlt

% Data Section
a_3		res 4  		% Declaring a : integer
b_4		res 4  		% Declaring b : integer
