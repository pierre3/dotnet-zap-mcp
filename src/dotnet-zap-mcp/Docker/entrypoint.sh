#!/bin/sh
mkdir -p /home/zap/.ZAP
cp /zap/wrk/template/zap-config.xml /home/zap/.ZAP/config.xml
sed -i "s/ZAP_API_KEY_PLACEHOLDER/${ZAP_API_KEY}/g" /home/zap/.ZAP/config.xml
exec "$@"
