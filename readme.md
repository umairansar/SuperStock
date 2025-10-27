### Phase 1

![img.png](img.png)

### Phase 2

![img_1.png](img_1.png)

### Phase 3

![img_2.png](img_2.png)

## Benchmarks 
### Phase 1
- 2686 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Db/Buy POST'`
- 7140 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Atomic/Buy POST'`
- 9872 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Signal/Buy POST'`
- 6496 `siege -c5 -t2s -b 'http://localhost:5059/api/v1/OneStock/Cache/Lock/Buy POST'`

## Docker Setup
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

## Phase ideas

Here are “bigger-picture” places you’d reach for Interlocked / Volatile—not just code snippets:

Hot-reloadable config / feature flags (snapshot publish)
Build a new immutable config object and Exchange it into place; readers Volatile.Read the latest snapshot without locks. (RCU / copy-on-write.)

In-process leader / ownership
Competing workers attempt CompareExchange(ref ownerThreadId, myId, 0). The winner becomes the single writer; others act as readers.
