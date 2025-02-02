
## 1. Shape of a frame

| **Frame delimiter** | **Length**    | Header | Mail    | Checksum |
| ------------------- | ------------- | ------ | ------- | -------- |
| 0x10                |               |        |         |          |
| 1 Byte              | 2 Bytes (MSB) | 1 Byte | ? Bytes | 1 Byte   |

### 1.1 Length
The length is exactly 2 bytes and must be transmitted most significant byte first.
Unlike what the name implies it only represents the length of the mail and header segments (minus the checksum and itself).

### 1.2 Header
The header is a crucial component of the frame, it is the foundation for the synchronization of the protocol, as it describes the type of frame present, plus it carries the PollFinal bit.

The structure of the header is as follows:

**Bit 7 I/C or 7..6 Sc/Uc :** 
Indicates whether the frame is an Information or Control type, Bit 6 may further narrow it down to
either Supervisor control or Unnumbered control type.

Possible combinations:

| 7   | 6   |
| --- | --- |
| 0   | 0   |
 Information

| 7   | 6   |
| --- | --- |
| 1   | 0   |
Supervisor control

| 7   | 6   |
| --- | --- |
| 1   | 1   |
Unnumbered control

**Bit 6..4 TxSeq:**
- Jump to [[#2.1 Information frame]]

**Bit 2..0 RxSeq:**
- Jump to [[#2.1 Information frame]]

**Bit 3 PollFinal:**
- When set to '1' it signals the other party a status is expected  (usually returned by a Supervisor control frame).

**Bit 5..4 Supervisor Id:**
- Supervisory frame identifier bits:
	1. '00' Receive ready
	2. '01' Reject
	3. '10' Receive not ready

**Bit 4..5 Unnumbered Id:**
- Unnumbered frame Id, only the following is valid:
	1. '00' (SABM) Set Asynchrnous Balanced Mode

#### 2. Types of frames:

- Information Frame
- Supervisory control frame
- Unnumbered control frame
##### 2.1 Information frame
The information frame is utilized to send data messages between the two parties (CVM and Host).
When an information frame is created a few things must be considered:
- if the PollFinal bit must be set (e.g. an API command may require it).
- the Rx/Tx sequence numbers.
Regarding the Rx/Tx sequence numbers, it's important that each party increments TxSeq, with exception; should it be necessary that a frame must be retransmitted.
RxSeq must also be taken into consideration, it should hold the expected sequence number to be received next.

**Header format:**

| 7   | 6..4  | 3   | 2..0  |
| --- | ----- | --- | ----- |
| 0   | TxSeq | PF  | RxSeq |

| 7   | 6..4  | 3   | 2..0  |
| --- | ----- | --- | ----- |
| 0   | TxSeq | PF  | RxSeq |
| 0   | 001   | 0   | 010   |
^Example

TxSet: 0x8 (valid bitmask for setting TxSet 1 when building the header)
RxSet: 0x2 (2 would be the expected sequence number after 1)

##### 2.2 Supervisory control frame
The supervisory control frame is used to acknowledge received packages by either party.
[[Protocol notes#1.2 Header]] Shows three possibilities, dependent on synchronization between the two parties (RxSeq) an appropriate acknowledgement will be sent.

**Header format:**

| 7..6 | 5..4 | 3   | 2..0 |
| ---- | ---- | --- | ---- |
| 10   | 00   | 0   | 000  |

| 7..6 | 5..4 | 3         | 2..0  |
| ---- | ---- | --------- | ----- |
| sC   | SuId | PollFinal | RxSeq |

##### 2.3 Unnumbered control frame
The unnumbered control frame is sent to initialize and reset communication between the two units. The SABM ([[Protocol notes#1.2 Header]]) will reset both Rx and Tx packet counters to 0.

**Header format:**

| 7..6 | 5..4 | 3   | 2..0 |
| ---- | ---- | --- | ---- |
| 11   | 00   | 0   | 000  |

| 7..6 | 5..4 | 3   | 2..0 |
| ---- | ---- | --- | ---- |
| Uc   | UnId | PF  |      |
