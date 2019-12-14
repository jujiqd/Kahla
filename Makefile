staging:
	docker build . -t aiursoftweb/kahla-server-api:staging
	docker push aiursoftweb/kahla-server-api:staging

production:
	docker build . -t aiursoftweb/kahla-server-api:production
	docker push aiursoftweb/kahla-server-api:production