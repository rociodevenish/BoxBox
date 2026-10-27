const target=`http://127.0.0.1:${process.env.F1_API_PORT??4571}`;
module.exports={'/api/**':{target,secure:false},'/hubs/**':{target,secure:false,ws:true}};
