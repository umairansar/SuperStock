![superstock_retro_transparent_noborder_noarrow](https://github.com/user-attachments/assets/7c8bdad4-4fe2-4eee-8155-fb8bd90c1b14)![]()

### Phase 1 ✅

<img width="927" height="401" alt="image" src="https://github.com/user-attachments/assets/54645661-6177-4a58-a987-8b344efc3602" /><br />

API(s):
- **`BuySafe`** Atomically decrements stock in database.
- **`BuyFastAtomic`** Atomically decrements stock in in-memory cache.
- **`BuyFastSignal`** Non-locking synchronization context to decrementing stock in in-memory cache. 
- **`BuyFastLocking`** Exclusive lock while decrementing stock in in-memory cache.

### Phase 2 ✅

<img width="965" height="376" alt="image" src="https://github.com/user-attachments/assets/368b9906-dc05-4a1d-b631-b34a89066775" /><br />

API(s):
- **`BuyFastAtomic`** Atomically decrements stock in Concurrent Dictionary.

Command(s):
- Navigate to directory SuperStock/Tests in terminal
- Run `k6 run k6.js`

### Phase 3 🚸

<img width="967" height="625" alt="image" src="https://github.com/user-attachments/assets/8e154de5-5004-4a86-b062-1627763341dc" /><br />

#### Redis Pub/Sub Notes

Redis Pub/Sub is stateless which means cache update events could be lost if either publisher, consumer, or Redis are down.  
Writes will still be consistent, as long as Writer node is up and running.  
Possibility of oversell in this case, since all the state is entirely maintained in memory.

#### Scenario

- Primary sells some tickets i.e. writes  
- Cache update events fail to be sent by Primary or accepted by Redis or consumer by Secondaries  
- Primary then goes down  
- Readers still have old stock counts for ticket in their cache  
- Last few writes by Primary are forever lost

#### Possible Solutions

- Use Write Ahead Log (WAL). Orchestrator synchronizes them during automatic failover. Perhaps we can access WAL for inaccessible Primary node via docker volumes. But that tighly couples Orchestrator with the SuperStock cluster (all 3 nodes). Orchestrator is still a single point of failure. Need to look into leaderless distributed election.

- Use Quorams. Primary publishes a message and majority nodes reply with success, then Primary commits the transaction i.e. Ticker Sold. But what happens if all replies got Success but Primary went down? Then all the majority nodes would have comitted the sell (I hope...), even though Primary did not commit. In this case, Orchestrator can detect that Primary node died and send TraceId to all Secondary nodes who will revert the stock for the corresponding TraceId. No need to worry about lost updates on Secondary Nodes, since all the subsequent writes after our TraceId are not possible since Primary was already down.

### Phase 4

<img width="958" height="587" alt="image" src="https://github.com/user-attachments/assets/12cd93af-7cbb-4bef-bde3-86303ab45acc" /><br />

### Phase 5

<img width="955" height="622" alt="image" src="https://github.com/user-attachments/assets/f04510bd-27ac-4f2b-bfcd-114e6d4e2405" /><br />

### Phase 6
<img width="951" height="452" alt="image" src="https://github.com/user-attachments/assets/428f5664-4792-430d-aea7-5b38c77e456b" /><br />

## Benchmarks 
### Phase 1
- 2686 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Db/Buy POST'`
- 7140 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Atomic/Buy POST'`
- 9872 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Signal/Buy POST'`
- 6496 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Lock/Buy POST'`

### Phase 2
<img width="987" height="802" alt="image" src="https://github.com/user-attachments/assets/0ae693e6-b3bc-4f2d-ac8c-22086d79bc1e" />

### Phase 3
#### Docker Setup
Build and run mongo db container

```json
docker pull mongo
docker run -d --name mongo-for-super-stock -p 27018:27017 mongo
```

Go to directory containing DockerFile and build:

```json
docker build --no-cache -t couter-image:latest -f Dockerfile .
```

Then run the container (3 copies)

```json
docker run -d --name core-counter -p 5059:5059 couter-image   
docker run -d --name core-counter-1 -p 5080:5059 couter-image
docker run -d --name core-counter-2 -p 5081:5059 couter-image
```

Stop and remove container

```json
docker rm -f core-counter
```

#### Redis Pub/Sub via Docker

- Primary instance publishes cache update events
- Secondary instances consume the message
- Primary ignores the consumption of echo message 
<img width="1429" height="399" alt="Screenshot 2025-11-01 at 3 43 54 AM" src="https://github.com/user-attachments/assets/bc83d89c-ca23-4c5c-88c1-cce174b1b34f" />

#### How to start containers with host id passed as environment variable?
```json
docker run -it -e STOCK_HOST_ID=SuperPrimary --name core-counter -p 5059:5059 couter-image
docker run -it -e STOCK_HOST_ID=SuperSecondary --name core-counter-1 -p 5080:5059 couter-image 
docker run -it -e STOCK_HOST_ID=DuperSeconfary --name core-counter-2 -p 5081:5059 couter-image
```

### Random ideas

Here are “bigger-picture” places you’d reach for Interlocked / Volatile—not just code snippets:

Hot-reloadable config / feature flags (snapshot publish)
Build a new immutable config object and Exchange it into place; readers Volatile.Read the latest snapshot without locks. (RCU / copy-on-write.)

In-process leader / ownership
Competing workers attempt CompareExchange(ref ownerThreadId, myId, 0). The winner becomes the single writer; others act as readers.
