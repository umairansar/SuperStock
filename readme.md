Phase 1

![img.png](img.png)

Phase 2

![img_1.png](img_1.png)

Phase 3

![img_2.png](img_2.png)

Phase ideas

Here are “bigger-picture” places you’d reach for Interlocked / Volatile—not just code snippets:

Hot-reloadable config / feature flags (snapshot publish)
Build a new immutable config object and Exchange it into place; readers Volatile.Read the latest snapshot without locks. (RCU / copy-on-write.)

In-process leader / ownership
Competing workers attempt CompareExchange(ref ownerThreadId, myId, 0). The winner becomes the single writer; others act as readers.
