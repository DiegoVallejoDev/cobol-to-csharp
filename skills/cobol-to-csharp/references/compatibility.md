# Compatibility questions to resolve from the user's source

These are investigation rules, not a supplied runtime or universal compiler
specification. Record the actual dialect/options and justify each decision.

| Feature | Required investigation |
|---------|------------------------|
| DISPLAY numbers | Sign placement/separation, overpunch table, encoding, padding, negative zero, malformed input |
| COMP / BINARY | Compiler-dependent storage width, endianness, alignment, signedness, truncation options |
| COMP-3 | Decimal digits, pad nibble for even digit counts, permitted sign nibbles, scale, invalid digits/signs |
| Arithmetic | Intermediate precision, receiving PIC range, rounding mode, truncation, overflow, ON SIZE ERROR |
| REDEFINES | Shared storage size, overlays, active interpretation, retained bytes |
| OCCURS | Bounds, strides, indices versus subscripts, DEPENDING ON and actual record length |
| MOVE / INITIALIZE | Elementary versus group semantics, category conversions, filler exclusions, VALUE clauses |
| Text | Actual code page, NATIONAL representation, collation, byte versus character lengths |
| CALL / LINKAGE | BY REFERENCE/CONTENT/VALUE, shared mutation, argument representation, return codes |
| Control flow | PERFORM THRU, fall-through, GO TO, paragraphs reached from multiple paths |
| Files | Organization, access mode, key ordering/duplicates, START, REWRITE, status codes, locks |
| Variable records | Actual descriptor format and whether its length includes the descriptor; compiler/runtime flags |
| JCL / services | DD mapping, PARM bytes, step conditions, utility behavior, transactions, CICS/DB/service boundaries |

COMP-3 generally stores two decimal digits per byte and a trailing sign nibble;
an even number of digits requires a pad nibble. Accepted signs and validation
policy must come from compiler/runtime evidence, not a single guessed table.

.NET decimal has finite precision/range. Check the entire PIC and arithmetic
path; use a scaled integer or appropriate explicit representation where needed.
Binary floating point is not a replacement for exact financial arithmetic.

Do not hard-code one corpus's record sizes, field offsets, RDW convention,
timestamp spans, encoding, or legacy bugs. No automatic CICS/VSAM/database
emulation is included. Define an adapter contract and unsupported behavior
instead of replacing a missing service with a passing stub.

Business modernization (changed rules, corrected bugs, new storage behavior)
is separate from a compatibility port. Obtain explicit approval and document
the intentional divergence and tests.
