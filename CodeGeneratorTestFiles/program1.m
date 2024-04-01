
% Execution Code
entry
		addi r12,r0,2		% Loading 2 into r12
		addi r11,r0,1		% Loading 1 into r11
		clt r10, r12, r11		% r12 < r11 = r10
ifthen1		bz r10,else1		% If r10 is false, jump to else1
		addi r10,r0,3		% Loading 3 into r10
		addi r11,r0,2		% Loading 2 into r11
		cgt r12, r10, r11		% r10 > r11 = r12
ifthen2		bz r12,else2		% If r12 is false, jump to else2
		lw r12,b_4(r0) 		% Loading b : integer
		addi r11,r0,4		% Loading 4 into r11
		add r12,r0,r11		% Assigning r11 to r12
		j endif2		% Jump to endif2
else2		 nop		% Start of the else block
endif2		 nop		% End of the if block
		j endif1		% Jump to endif1
else1		 nop		% Start of the else block
gowhile1		 nop		% Start of the while condition block
		addi r12,r0,1		% Loading 1 into r12
		addi r11,r0,2		% Loading 2 into r11
		clt r10, r12, r11		% r12 < r11 = r10
		bz r10,endwhile1		% If r10 is false, jump to endwhile1
		lw r10,b_4(r0) 		% Loading b : integer
		lw r11,b_4(r0) 		% Loading b : integer
		addi r12,r0,2		% Loading 2 into r12
		add r9, r11, r12		% r11 + r12 = r9
		add r10,r0,r9		% Assigning r9 to r10
		j gowhile1		% Jump to gowhile1
endwhile1		 nop		% End of the while block
endif1		 nop		% End of the if block
hlt

% Data Section
a_3		res 4  		% Declaring a : integer
b_4		res 4  		% Declaring b : integer
