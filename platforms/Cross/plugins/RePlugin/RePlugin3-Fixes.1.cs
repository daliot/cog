'From Squeak 3.2 of 11 July 2002 [latest update: #4917] on 17 August 2002 at 6:10:11 pm'!
"Change Set:		RePlugin3-Fixes
Date:			17 August 2002
Author:			tim@sumeru.stanford.edu

Some small fixes to RePlugin3 to allow compiling on Acorn"!


!RePlugin methodsFor: 're primitives' stamp: 'tpr 8/17/2002 18:00'!
primPCREExec

"<rcvr primPCREExec: searchObject>, where rcvr is an object with instance variables:

	'patternStr compileFlags pcrePtr extraPtr errorStr errorOffset matchFlags'	

Apply the regular expression (stored in <pcrePtr> and <extratr>, generated from calls to primPCRECompile), to smalltalk String searchObject using <matchOptions>.  If there is no match, answer nil.  Otherwise answer a ByteArray of offsets representing the results of the match."

	| searchObject searchBuffer length  result matchSpacePtr matchSpaceSize |
	self export: true.
	self var:#searchBuffer	declareC: 'char *searchBuffer'.
	self var:#matchSpacePtr	declareC: 'int *matchSpacePtr'.
	self var:#result			declareC: 'int result'.
	
	"Load Parameters"
	searchObject _ interpreterProxy stackObjectValue: 0.	
	searchBuffer _ interpreterProxy arrayValueOf: searchObject.
	length _ interpreterProxy byteSizeOf: searchObject.
	self loadRcvrFromStackAt: 1.
	"Load Instance Variables"
	pcrePtr _ self rcvrPCREBufferPtr.
	extraPtr _ self rcvrExtraPtr.
	matchFlags _ self rcvrMatchFlags.
	matchSpacePtr _ self rcvrMatchSpacePtr.
	matchSpaceSize _ self rcvrMatchSpaceSize.

	interpreterProxy failed ifTrue:[^ nil].
	
	result _ self 
		cCode: 'pcre_exec((pcre *)pcrePtr, (pcre_extra *)extraPtr, 
				searchBuffer, length, 0, matchFlags, matchSpacePtr, matchSpaceSize)'.

	interpreterProxy pop: 2; pushInteger: result.

	"empty call so compiler doesn't bug me about variables not used"
	self touch: searchBuffer; touch: matchSpacePtr; touch: matchSpaceSize; touch: length
! !

!RePlugin methodsFor: 're primitives' stamp: 'tpr 8/17/2002 18:00'!
primPCREExecfromto

"<rcvr primPCREExec: searchObject> from: fromInteger to: toInteger>, where rcvr is an object with instance variables:

	'patternStr compileFlags pcrePtr extraPtr errorStr errorOffset matchFlags'	

Apply the regular expression (stored in <pcrePtr> and <extratr>, generated from calls to primPCRECompile), to smalltalk String searchObject using <matchOptions>, beginning at offset <fromInteger> and continuing until offset <toInteger>.  If there is no match, answer nil.  Otherwise answer a ByteArray of offsets representing the results of the match."

	| searchObject searchBuffer length  result matchSpacePtr matchSpaceSize fromInteger toInteger |
	self export: true.
	self var:#searchBuffer	declareC: 'char *searchBuffer'.
	self var:#fromInteger declareC: 'int fromInteger'.
	self var:#toInteger declareC: 'int toInteger'.
	self var:#matchSpacePtr	declareC: 'int *matchSpacePtr'.
	self var:#result			declareC: 'int result'.
	
	"Load Parameters"
	toInteger _ interpreterProxy stackIntegerValue: 0.
	fromInteger _ interpreterProxy stackIntegerValue: 1.
	searchObject _ interpreterProxy stackObjectValue: 2.	
	searchBuffer _ interpreterProxy arrayValueOf: searchObject.
	length _ interpreterProxy byteSizeOf: searchObject.
	self loadRcvrFromStackAt: 3.

	"Validate parameters"
	interpreterProxy success: (1 <= fromInteger).
	interpreterProxy success: (toInteger<=length).
	fromInteger _ fromInteger - 1. "Smalltalk offsets are 1-based"
	interpreterProxy success: (fromInteger<=toInteger).

	"adjust length, searchBuffer"
	length _ toInteger - fromInteger.
	searchBuffer _ searchBuffer + fromInteger.

	"Load Instance Variables"
	pcrePtr _ self rcvrPCREBufferPtr.
	extraPtr _ self rcvrExtraPtr.
	matchFlags _ self rcvrMatchFlags.
	matchSpacePtr _ self rcvrMatchSpacePtr.
	matchSpaceSize _ self rcvrMatchSpaceSize.
	interpreterProxy failed ifTrue:[^ nil].
	
	result _ self 
		cCode: 'pcre_exec((pcre *)pcrePtr, (pcre_extra *)extraPtr, 
				searchBuffer, length, 0, matchFlags, matchSpacePtr, matchSpaceSize)'.
	interpreterProxy pop: 2; pushInteger: result.

	"empty call so compiler doesn't bug me about variables not used"
	self touch: searchBuffer; touch: matchSpacePtr; touch: matchSpaceSize; touch: length
! !

!RePlugin methodsFor: 'rcvr linkage' stamp: 'tpr 8/17/2002 18:01'!
allocateStringAndSetRcvrErrorStrFromCStr: aCStrBuffer

	|length errorStrObj errorStrObjPtr |
	self var: #aCStrBuffer declareC: 'const char *aCStrBuffer'.
	self var: #errorStrObjPtr declareC: 'void *errorStrObjPtr'.
	"Allocate errorStrObj"
	length _ self cCode: 'strlen(aCStrBuffer)'.
	errorStrObj _ interpreterProxy
				instantiateClass: (interpreterProxy classString) 
				indexableSize: length.
	self loadRcvrFromStackAt: 0. "Assume garbage collection after instantiation"

	"Copy aCStrBuffer to errorStrObj's buffer"
	errorStrObjPtr _ interpreterProxy arrayValueOf: errorStrObj.	
	self cCode:'memcpy(errorStrObjPtr,aCStrBuffer,length)'.
	self touch: errorStrObjPtr; touch: errorStrObj.
	"Set rcvrErrorStr from errorStrObj and Return"
	self rcvrErrorStrFrom: errorStrObj.
	^errorStrObj.! !


!RePlugin class methodsFor: 'plugin code generation' stamp: 'tpr 8/17/2002 18:02'!
declareCVarsIn: cg

	cg addHeaderFile:'"rePlugin.h"'.

	"Memory Managament Error Checking"
	cg var: 'netMemory' 	declareC: 'int netMemory = 0'.
	cg var: 'numAllocs' 	declareC: 'int numAllocs = 0'.
	cg var: 'numFrees' 		declareC: 'int numFrees = 0'.
	cg var: 'lastAlloc'		declareC: 'int lastAlloc = 0'.

	"The receiver Object Pointer"
	cg var: 'rcvr'			declareC: 'int rcvr'.

	"Instance Variables of Receiver Object"
	cg var: 'patternStr'		declareC: 'int patternStr'.
	cg var: 'compileFlags'	declareC: 'int compileFlags'.
	cg var: 'pcrePtr'		declareC: 'int pcrePtr'.
	cg var: 'extraPtr'		declareC: 'int extraPtr'.
	cg var: 'errorStr'		declareC: 'int errorStr'.
	cg var: 'errorOffset'	declareC: 'int errorOffset'.
	cg var: 'matchFlags'	declareC: 'int matchFlags'.

	"Support Variables for Access to Receiver Instance Variables"
	cg var: 'patternStrPtr' declareC: 'const char * patternStrPtr'.
	cg var: 'errorStrBuffer'	declareC: 'const char * errorStrBuffer'.! !

