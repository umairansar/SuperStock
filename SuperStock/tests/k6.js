import http from 'k6/http'
import { SharedArray } from 'k6/data';
import { check } from 'k6';

const tickers = new SharedArray('tickers', function() {
    return ['NVDA', 'TSLA', 'AMD']
})

export function setup() {
    const responses = http.batch([
        ['GET', `http://localhost:5059/api/v1/ManyStock/Cache/PeekFastAtomic/${tickers[0]}`],
        ['GET', `http://localhost:5059/api/v1/ManyStock/Cache/PeekFastAtomic/${tickers[1]}`],
        ['GET', `http://localhost:5059/api/v1/ManyStock/Cache/PeekFastAtomic/${tickers[2]}`],
    ]);
    
    const initialStock = {}
    const initialSold = {}
    
    initialStock[tickers[0]] = responses[0].json().remainingStock;
    initialStock[tickers[1]] = responses[1].json().remainingStock;
    initialStock[tickers[2]] = responses[2].json().remainingStock;

    initialSold[tickers[0]] = responses[0].json().soldStock;
    initialSold[tickers[1]] = responses[1].json().soldStock;
    initialSold[tickers[2]] = responses[2].json().soldStock;

    console.log(`[Initial] ${tickers[0]} = ${initialStock[tickers[0]]}`);
    console.log(`[Initial] ${tickers[1]} = ${initialStock[tickers[1]]}`);
    console.log(`[Initial] ${tickers[2]} = ${initialStock[tickers[2]]}`);
    return { initialStock, initialSold };
}

export const options = {
    stages: [
        { duration: '2s', target: 5 },
        { duration: '1s', target: 2 },
    ],
};

export default () => {
    const i = Math.floor(Math.random() * tickers.length);
    const ticker = tickers[i];
    const res = http.post(`http://localhost:5059/api/v1/ManyStock/Cache/BuyFastAtomic/${ticker}`);
    check(res, {'Ticker Bought Successfully' : r => r.status === 200 });
}

export function teardown(data) {
    const { initialStock, initialSold } = data;
    const responses = http.batch([
        ['GET', `http://localhost:5059/api/v1/ManyStock/Cache/PeekFastAtomic/${tickers[0]}`],
        ['GET', `http://localhost:5059/api/v1/ManyStock/Cache/PeekFastAtomic/${tickers[1]}`],
        ['GET', `http://localhost:5059/api/v1/ManyStock/Cache/PeekFastAtomic/${tickers[2]}`],
    ]);
    
    console.log(`[Sold] ${tickers[0]} : ${Number(responses[0].json().soldStock) - Number(initialSold[tickers[0]])}`);
    console.log(`[Sold] ${tickers[1]} : ${Number(responses[1].json().soldStock) - Number(initialSold[tickers[1]])}`);
    console.log(`[Sold] ${tickers[2]} : ${Number(responses[2].json().soldStock) - Number(initialSold[tickers[2]])}`);
    console.log(`[Remaining] ${tickers[0]} : ${responses[0].json().remainingStock}`);
    console.log(`[Remaining] ${tickers[1]} : ${responses[1].json().remainingStock}`);
    console.log(`[Remaining] ${tickers[2]} : ${responses[2].json().remainingStock}`);

    check(responses[0], {'No Oversell / Undersell' : r => r.json().remainingStock + r.json().soldStock === initialStock[tickers[0]] + initialSold[tickers[0]]});
    check(responses[1], {'No Oversell / Undersell' : r => r.json().remainingStock + r.json().soldStock === initialStock[tickers[1]] + initialSold[tickers[1]]});
    check(responses[2], {'No Oversell / Undersell' : r => r.json().remainingStock + r.json().soldStock === initialStock[tickers[2]] + initialSold[tickers[2]]});
}