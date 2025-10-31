### Phase 1 ✅

<img width="907" height="400" alt="image" src="https://github.com/user-attachments/assets/7a21db70-85e4-414b-9e81-b97a883ecaa7" />

### Phase 2 🚸

<img width="950" height="401" alt="image" src="https://github.com/user-attachments/assets/f99bf319-dde6-4611-82b2-f4444f9110e5" />

### Phase 3 🚸

<img width="936" height="565" alt="image" src="https://github.com/user-attachments/assets/6ea4bc9d-61f2-4784-8bb0-3368a00ede7f" />

### Phase 4

<img width="951" height="477" alt="image" src="https://github.com/user-attachments/assets/828c8ffc-dce3-4daf-86df-ff3b94f990d3" />

## Benchmarks 
### Phase 1
- 2686 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Db/Buy POST'`
- 7140 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Atomic/Buy POST'`
- 9872 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Signal/Buy POST'`
- 6496 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Lock/Buy POST'`

### Phase 2
<img width="987" height="802" alt="image" src="https://github.com/user-attachments/assets/0ae693e6-b3bc-4f2d-ac8c-22086d79bc1e" />

## Docker Setup (Phase 3)
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

## Random ideas

Here are “bigger-picture” places you’d reach for Interlocked / Volatile—not just code snippets:

Hot-reloadable config / feature flags (snapshot publish)
Build a new immutable config object and Exchange it into place; readers Volatile.Read the latest snapshot without locks. (RCU / copy-on-write.)

In-process leader / ownership
Competing workers attempt CompareExchange(ref ownerThreadId, myId, 0). The winner becomes the single writer; others act as readers.
